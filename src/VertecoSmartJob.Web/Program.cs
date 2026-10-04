using Microsoft.AspNetCore.Authentication.Cookies;
using VertecoSmartJob.Web.Api;
using VertecoSmartJob.Web.Auth;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(
        options =>
            options.Filters.Add<
                ApiUnauthorizedExceptionFilter>());

builder.Services
    .AddHttpContextAccessor();

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults
            .AuthenticationScheme)
    .AddCookie(
        options =>
        {
            options.LoginPath =
                "/Account/Login";

            options.AccessDeniedPath =
                "/Account/Login";

            options.ExpireTimeSpan =
                TimeSpan.FromHours(8);

            options.SlidingExpiration =
                true;

            if (builder.Environment
                .IsProduction())
            {
                options.Cookie.SecurePolicy =
                    CookieSecurePolicy.Always;

                options.Cookie.HttpOnly =
                    true;

                options.Cookie.SameSite =
                    SameSiteMode.Lax;
            }
        });

builder.Services.AddAuthorization();

var apiBaseUrl =
    builder.Configuration[
        "Api:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Api:BaseUrl is not configured.");

if (builder.Environment.IsProduction())
{
    ValidateProductionApiUrl(
        apiBaseUrl);
}

builder.Services
    .AddTransient<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<AuthApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl));

builder.Services
    .AddHttpClient<JobCardsApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<CustomersApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<SitesApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<JobTemplatesApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<TechniciansApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<DashboardApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<UsersApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<CorrectiveActionsApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<ReportsApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

builder.Services
    .AddHttpClient<AuditApiClient>(
        client =>
            client.BaseAddress =
                new Uri(apiBaseUrl))
    .AddHttpMessageHandler<
        ApiKeyDelegatingHandler>();

var app =
    builder.Build();

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

        context.Response.Headers.Append(
            "Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src https://fonts.gstatic.com; " +
            "img-src 'self' data:; " +
            "connect-src 'self';");

        await next();
    });

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapGet(
    "/health/live",
    () =>
        Results.Ok(
            new
            {
                status =
                    "Healthy",

                service =
                    "VertecoSmartJob.Web",

                timestampUtc =
                    DateTime.UtcNow
            }));

app.MapControllerRoute(
    name:
        "default",

    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void ValidateProductionApiUrl(
    string apiBaseUrl)
{
    if (!Uri.TryCreate(
            apiBaseUrl,
            UriKind.Absolute,
            out Uri? uri) ||
        !string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase) ||
        uri.IsLoopback)
    {
        throw new InvalidOperationException(
            "Api__BaseUrl must be configured with the production HTTPS SmartJob API URL.");
    }
}