using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Files;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Application.Management;
using VertecoSmartJob.Application.Reports;
using VertecoSmartJob.Infrastructure.Ai;
using VertecoSmartJob.Infrastructure.Email;
using VertecoSmartJob.Infrastructure.Files;
using VertecoSmartJob.Infrastructure.Identity;
using VertecoSmartJob.Infrastructure.Notifications;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Repositories;

namespace VertecoSmartJob.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool testing = false)
    {
        var connectionString =
            configuration.GetConnectionString(
                "AzureSql");

        if (string.IsNullOrWhiteSpace(
                connectionString) &&
            !testing)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:AzureSql is required.");
        }

        services.AddDbContext<AppDbContext>(
            options =>
                options.UseSqlServer(
                    testing &&
                    string.IsNullOrWhiteSpace(
                        connectionString)
                        ? "Server=localhost;Database=SmartJobTest;Trusted_Connection=True;"
                        : connectionString,
                    sql =>
                        sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(
            sp =>
                sp.GetRequiredService<AppDbContext>());

        services.AddSingleton(sp =>
        {
            var storageConnection =
                configuration[
                    "Blob:ConnectionString"];

            if (!string.IsNullOrWhiteSpace(
                    storageConnection))
            {
                return new BlobServiceClient(
                    storageConnection);
            }

            var serviceUri =
                configuration[
                    "Blob:ServiceUri"];

            if (string.IsNullOrWhiteSpace(
                    serviceUri))
            {
                throw new InvalidOperationException(
                    "Blob:ServiceUri or Blob:ConnectionString is required.");
            }

            return new BlobServiceClient(
                new Uri(
                    serviceUri.TrimEnd('/')),
                new DefaultAzureCredential());
        });

        services.AddSingleton(
            sp =>
                sp.GetRequiredService<
                        BlobServiceClient>()
                    .GetBlobContainerClient(
                        configuration[
                            "Blob:Container"]
                        ?? "job-evidence"));

        services.AddScoped<
            IFieldFileStore,
            AzureBlobFileStore>();

        services.AddScoped<
            IJobCardRepository,
            JobCardRepository>();

        services.AddScoped<
            IMobileSyncRepository,
            MobileSyncRepository>();

        services.AddScoped<
            ICustomerRepository,
            CustomerRepository>();

        services.AddScoped<
            IUserRepository,
            UserRepository>();

        services.AddScoped<
            IAuditLogRepository,
            AuditLogRepository>();

        services.AddScoped<
            ISiteRepository,
            SiteRepository>();

        services.AddScoped<
            ITechnicianRepository,
            TechnicianRepository>();

        services.AddScoped<
            IJobTemplateRepository,
            JobTemplateRepository>();

        services.AddScoped<
            IPasswordResetTokenRepository,
            PasswordResetTokenRepository>();

        services.AddScoped<
            IRefreshTokenRepository,
            RefreshTokenRepository>();

        services.AddScoped<JobCardService>();

        // Handles pull and push requests
        // from the mobile app.
        services.AddScoped<
            MobileSyncService>();

        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<
            CustomerService>();

        services.AddScoped<
            SiteService>();

        services.AddScoped<
            UserManagementService>();

        services.AddScoped<
            TechnicianManagementService>();

        services.AddScoped<
            ReportService>();

        services.AddSingleton<
            IReportingAiService>(
            sp =>
                new GeminiReportingAiService(
                    new HttpClient
                    {
                        Timeout =
                            TimeSpan.FromSeconds(25)
                    },
                    configuration,
                    sp.GetRequiredService<
                        Microsoft.Extensions.Logging
                            .ILogger<
                                GeminiReportingAiService>>()));

        services.AddSingleton<
            PasswordHashingService>();

        services.AddSingleton<
            JwtTokenService>();

        services.AddScoped<
            IEmailSender,
            SmtpEmailSender>();

        services.AddScoped<
            INotificationService,
            EmailNotificationService>();

        services.AddScoped<
            ISubmissionValidationStrategy,
            RequiredFieldsStrategy>();

        services.AddScoped<
            ISubmissionValidationStrategy,
            EvidencePresenceStrategy>();

        services.AddScoped<
            ISubmissionValidationStrategy,
            SignaturePresenceStrategy>();

        return services;
    }
}