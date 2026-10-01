using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Infrastructure;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Seed;
using VertecoSmartJob.Api.Middleware;
using VertecoSmartJob.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using VertecoSmartJob.Application.Abstractions;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Production authentication must be added by the identity workstream before hosting.");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    var keyDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys"));
    keyDirectory.Create();
    builder.Services.AddDataProtection().PersistKeysToFileSystem(keyDirectory);
}

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsEnvironment("Testing"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "SmartJobScheme";
    options.DefaultChallengeScheme = "SmartJobScheme";
})
.AddPolicyScheme("SmartJobScheme", "API Key or Bearer", options =>
{
    options.ForwardDefaultSelector = context =>
        context.Request.Headers.ContainsKey("Authorization")
            ? Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme
            : ApiKeyAuthenticationHandler.SchemeName;
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { })
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!))
    };
});
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Verteco SmartJob API",
        Version = "v1",
        Description = "Job card lifecycle, scheduling and reporting API for Verteco field teams."
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        var origins = builder.Configuration.GetSection("WebAppOrigins").Get<string[]>()
            ?? ["https://localhost:7001", "http://localhost:5001"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ai", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 20;
        o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(5);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verteco SmartJob API v1"));
}

app.UseHttpsRedirection();
app.UseCors("Web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsEnvironment("Testing")) await db.Database.EnsureCreatedAsync();
    else await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();

public partial class Program; // exposed for WebApplicationFactory in integration tests
