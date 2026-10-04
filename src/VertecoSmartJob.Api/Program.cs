using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Azure.Storage.Blobs;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using VertecoSmartJob.Api.Auth;
using VertecoSmartJob.Api.Controllers.Mobile;
using VertecoSmartJob.Api.Middleware;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Infrastructure;
using VertecoSmartJob.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Production is now supported, but the application will
// refuse to start if required production configuration
// is missing or still points to localhost.
if (builder.Environment.IsProduction())
{
    ValidateProductionConfiguration(
        builder.Configuration);
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (builder.Environment.IsDevelopment())
{
    var keyDirectory =
        new DirectoryInfo(
            Path.Combine(
                builder.Environment.ContentRootPath,
                "App_Data",
                "keys"));

    keyDirectory.Create();

    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(
            keyDirectory);
}

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions
            .Converters
            .Add(
                new JsonStringEnumConverter());
    });

builder.Services
    .AddFluentValidationAutoValidation();

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment
        .IsEnvironment("Testing"));

builder.Services
    .AddSingleton<MobileUploadTokenService>();

builder.Services
    .AddHttpContextAccessor();

builder.Services
    .AddScoped<
        ICurrentUser,
        HttpCurrentUser>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            "SmartJobScheme";

        options.DefaultChallengeScheme =
            "SmartJobScheme";
    })
    .AddPolicyScheme(
        "SmartJobScheme",
        "API Key or Bearer",
        options =>
        {
            options.ForwardDefaultSelector =
                context =>
                    context.Request.Headers
                        .ContainsKey(
                            "Authorization")
                        ? Microsoft.AspNetCore
                            .Authentication
                            .JwtBearer
                            .JwtBearerDefaults
                            .AuthenticationScheme
                        : ApiKeyAuthenticationHandler
                            .SchemeName;
        })
    .AddScheme<
        AuthenticationSchemeOptions,
        ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler
            .SchemeName,
        _ =>
        {
        })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new Microsoft.IdentityModel.Tokens
                .TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidIssuer =
                    builder.Configuration[
                        "Jwt:Issuer"],

                ValidateAudience = true,

                ValidAudience =
                    builder.Configuration[
                        "Jwt:Audience"],

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new Microsoft.IdentityModel.Tokens
                        .SymmetricSecurityKey(
                            System.Text.Encoding.UTF8
                                .GetBytes(
                                    builder.Configuration[
                                        "Jwt:SigningKey"]!))
            };

        options.Events =
            new Microsoft.AspNetCore.Authentication
                .JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed =
                    context =>
                    {
                        var logger =
                            context.HttpContext
                                .RequestServices
                                .GetRequiredService<
                                    ILoggerFactory>()
                                .CreateLogger(
                                    "JwtBearer");

                        logger.LogWarning(
                            context.Exception,
                            "JWT authentication failed: {Message}",
                            context.Exception.Message);

                        return Task.CompletedTask;
                    }
            };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddEndpointsApiExplorer();

builder.Services
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title =
                    "Verteco SmartJob API",

                Version =
                    "v1",

                Description =
                    "Job card lifecycle, scheduling and reporting API for Verteco field teams."
            });

        c.AddSecurityDefinition(
            "SmartJobKey",
            new OpenApiSecurityScheme
            {
                Type =
                    SecuritySchemeType.ApiKey,

                In =
                    ParameterLocation.Header,

                Name =
                    "X-SmartJob-Key",

                Description =
                    "Development API key. Enter the key value without a header name."
            });

        c.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Type =
                    SecuritySchemeType.Http,

                Scheme =
                    "bearer",

                BearerFormat =
                    "JWT",

                Description =
                    "JWT from POST /api/v1/auth/login. Enter the token without the Bearer prefix."
            });

        // Separate requirements mean either
        // credential can authorize a protected request.
        c.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "SmartJobKey",
                            document)
                    ] = []
                });

        c.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "Bearer",
                            document)
                    ] = []
                });
    });

builder.Services
    .AddExceptionHandler<
        GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "Web",
            policy =>
            {
                var origins =
                    builder.Configuration
                        .GetSection(
                            "WebAppOrigins")
                        .Get<string[]>()
                    ??
                    [
                        "https://localhost:7001",
                        "http://localhost:5001"
                    ];

                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });

builder.Services.AddRateLimiter(
    options =>
    {
        options.AddFixedWindowLimiter(
            "ai",
            o =>
            {
                o.Window =
                    TimeSpan.FromMinutes(1);

                o.PermitLimit = 20;

                o.QueueLimit = 0;
            });

        options.AddFixedWindowLimiter(
            "auth",
            o =>
            {
                o.Window =
                    TimeSpan.FromMinutes(5);

                o.PermitLimit = 10;

                o.QueueLimit = 0;
            });

        options.AddFixedWindowLimiter(
            "evidence",
            o =>
            {
                o.Window =
                    TimeSpan.FromMinutes(1);

                o.PermitLimit = 10;

                o.QueueLimit = 0;
            });

        options.RejectionStatusCode =
            StatusCodes
                .Status429TooManyRequests;
    });

var app = builder.Build();

app.Use(
    async (context, next) =>
    {
        context.Response.Headers.Append(
            "X-Content-Type-Options",
            "nosniff");

        context.Response.Headers.Append(
            "X-Frame-Options",
            "DENY");

        context.Response.Headers.Append(
            "Referrer-Policy",
            "strict-origin-when-cross-origin");

        await next();
    });

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(
        c =>
            c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Verteco SmartJob API v1"));
}

app.UseHttpsRedirection();

app.UseCors("Web");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Lightweight liveness endpoint.
// This confirms that the API process is running.
app.MapGet(
    "/health/live",
    () =>
        Results.Ok(
            new
            {
                status = "Healthy",
                service =
                    "VertecoSmartJob.Api",
                timestampUtc =
                    DateTime.UtcNow
            }));

// Readiness confirms that the API can reach
// both its SQL database and Blob Storage.
app.MapGet(
    "/health/ready",
    async (
        AppDbContext db,
        BlobContainerClient blobContainer,
        ILoggerFactory loggerFactory,
        CancellationToken ct) =>
    {
        try
        {
            bool databaseAvailable =
                await db.Database
                    .CanConnectAsync(ct);

            bool blobAvailable =
                (await blobContainer
                    .ExistsAsync(ct))
                    .Value;

            if (databaseAvailable &&
                blobAvailable)
            {
                return Results.Ok(
                    new
                    {
                        status =
                            "Healthy",

                        database =
                            "Available",

                        blobStorage =
                            "Available",

                        timestampUtc =
                            DateTime.UtcNow
                    });
            }

            return Results.Json(
                new
                {
                    status =
                        "Unhealthy",

                    database =
                        databaseAvailable
                            ? "Available"
                            : "Unavailable",

                    blobStorage =
                        blobAvailable
                            ? "Available"
                            : "Unavailable",

                    timestampUtc =
                        DateTime.UtcNow
                },
                statusCode:
                    StatusCodes
                        .Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            loggerFactory
                .CreateLogger(
                    "Readiness")
                .LogError(
                    ex,
                    "SmartJob readiness check failed.");

            return Results.Json(
                new
                {
                    status =
                        "Unhealthy",

                    timestampUtc =
                        DateTime.UtcNow
                },
                statusCode:
                    StatusCodes
                        .Status503ServiceUnavailable);
        }
    });

// Development applies migrations automatically.
// Production migrations will be handled explicitly
// by the deployment pipeline instead.
if (app.Environment.IsDevelopment() ||
    app.Environment.IsEnvironment(
        "Testing"))
{
    using var scope =
        app.Services.CreateScope();

    var db =
        scope.ServiceProvider
            .GetRequiredService<
                AppDbContext>();

    if (app.Environment
        .IsEnvironment("Testing"))
    {
        await db.Database
            .EnsureCreatedAsync();
    }
    else
    {
        await db.Database
            .MigrateAsync();
    }
}

app.Run();

static void ValidateProductionConfiguration(
    IConfiguration configuration)
{
    var missingOrInvalid =
        new List<string>();

    string? connectionString =
        configuration
            .GetConnectionString(
                "AzureSql");

    if (string.IsNullOrWhiteSpace(
            connectionString))
    {
        missingOrInvalid.Add(
            "ConnectionStrings__AzureSql");
    }

    string? signingKey =
        configuration[
            "Jwt:SigningKey"];

    if (string.IsNullOrWhiteSpace(
            signingKey) ||
        signingKey.Length < 32)
    {
        missingOrInvalid.Add(
            "Jwt__SigningKey (minimum 32 characters)");
    }

    if (string.IsNullOrWhiteSpace(
            configuration[
                "Jwt:Issuer"]))
    {
        missingOrInvalid.Add(
            "Jwt__Issuer");
    }

    if (string.IsNullOrWhiteSpace(
            configuration[
                "Jwt:Audience"]))
    {
        missingOrInvalid.Add(
            "Jwt__Audience");
    }

    string? webBaseUrl =
        configuration[
            "WebApp:BaseUrl"];

    if (!IsProductionHttpsUri(
            webBaseUrl))
    {
        missingOrInvalid.Add(
            "WebApp__BaseUrl (must be a non-local HTTPS URL)");
    }

    string[] origins =
        configuration
            .GetSection(
                "WebAppOrigins")
            .Get<string[]>()
        ?? [];

    if (origins.Length == 0 ||
        origins.Any(
            origin =>
                !IsProductionHttpsUri(
                    origin)))
    {
        missingOrInvalid.Add(
            "WebAppOrigins (all production origins must use non-local HTTPS URLs)");
    }

    string? blobConnectionString =
        configuration[
            "Blob:ConnectionString"];

    string? blobServiceUri =
        configuration[
            "Blob:ServiceUri"];

    if (string.IsNullOrWhiteSpace(
            blobConnectionString) &&
        string.IsNullOrWhiteSpace(
            blobServiceUri))
    {
        missingOrInvalid.Add(
            "Blob__ServiceUri or Blob__ConnectionString");
    }
    else if (
        string.IsNullOrWhiteSpace(
            blobConnectionString) &&
        !IsProductionHttpsUri(
            blobServiceUri))
    {
        missingOrInvalid.Add(
            "Blob__ServiceUri (must be HTTPS)");
    }

    if (missingOrInvalid.Count > 0)
    {
        throw new InvalidOperationException(
            "SmartJob production configuration is incomplete or invalid:" +
            Environment.NewLine +
            " - " +
            string.Join(
                Environment.NewLine + " - ",
                missingOrInvalid));
    }
}

static bool IsProductionHttpsUri(
    string? value)
{
    if (!Uri.TryCreate(
            value,
            UriKind.Absolute,
            out Uri? uri))
    {
        return false;
    }

    return
        string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase) &&
        !uri.IsLoopback;
}

public partial class Program;