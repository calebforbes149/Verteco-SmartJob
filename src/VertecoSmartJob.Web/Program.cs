using Microsoft.AspNetCore.Authentication.Cookies;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is not configured (see appsettings.json).");

builder.Services.AddTransient<ApiKeyDelegatingHandler>();

builder.Services.AddHttpClient<JobCardsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<CustomersApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<SitesApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<JobTemplatesApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<TechniciansApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<DashboardApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<UsersApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<CorrectiveActionsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<ReportsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddHttpClient<AuditApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();