using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Application.Reports;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Repositories;
using VertecoSmartJob.Infrastructure.Persistence.Seed;
using VertecoSmartJob.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace VertecoSmartJob.Tests.Integration;

public class JobCardPersistenceTests
{
    [Fact]
    public async Task Lifecycle_PersistsSubmittedRevisionAuditAndReportingCounts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var customer = await db.Customers.Include(c => c.Sites).FirstAsync();
        var site = customer.Sites.First();
        var technician = await db.Technicians.FirstAsync();
        var repo = new JobCardRepository(db);
        var service = new JobCardService(repo, new TechnicianRepository(db),
            new JobTemplateRepository(db), new CustomerRepository(db), new SiteRepository(db),
            new AuditLogRepository(db),
            new NoOpNotificationService(),
            new ISubmissionValidationStrategy[]
            {
                new RequiredFieldsStrategy(), new EvidencePresenceStrategy(), new SignaturePresenceStrategy()
            }, db);
        var reportService = new ReportService(repo, new GeminiReportingAiService(new HttpClient(),
            new ConfigurationBuilder().AddInMemoryCollection().Build(), NullLogger<GeminiReportingAiService>.Instance));
        var now = DateTime.UtcNow;
        var before = await reportService.GetStatisticsAsync(now.Year, now.Month);

        var job = await service.CreateAsync(customer.Id, site.Id, JobPriority.Medium,
            DateOnly.FromDateTime(DateTime.UtcNow), "Test job", technician.Id);
        await service.AcceptAsync(job.Id, technician.Id);
        await service.StartAsync(job.Id, technician.Id);
        foreach (var item in job.ChecklistItems)
            await service.ToggleChecklistItemAsync(job.Id, item.Id, technician.Id);
        await service.AddEvidenceAsync(job.Id, technician.Id, "fixture.png:SHA256", "Site photo");
        await service.AddSignatureAsync(job.Id, technician.Id, technician.FullName, "Technician", "tech.png:SHA256");
        await service.AddSignatureAsync(job.Id, technician.Id, "Site representative", "Client", "client.png:SHA256");
        await service.SubmitForReviewAsync(job.Id, technician.Id);
        await service.CompleteAsync(job.Id, "Verified");

        db.ChangeTracker.Clear();
        var loaded = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(loaded);
        Assert.Equal(JobStatus.Completed, loaded.Status);
        Assert.Single(loaded.Revisions);
        Assert.True(loaded.Revisions[0].VerifyIntegrity());
        Assert.Equal(64, loaded.Revisions[0].ContentHash.Length);
        Assert.Equal(3, await db.AuditLogs.CountAsync(a => a.JobCardId == job.Id &&
            (a.Action == "JobSubmitted" || a.Action == "JobApproved" || a.Action == "JobCreated")));

        var report = await reportService.GetStatisticsAsync(now.Year, now.Month);
        Assert.Contains("Completed", report.StatusCounts.Keys);
        Assert.Equal(before.JobsCreated + 1, report.JobsCreated);
        Assert.Equal(before.JobsCompleted + 1, report.JobsCompleted);
    }
}