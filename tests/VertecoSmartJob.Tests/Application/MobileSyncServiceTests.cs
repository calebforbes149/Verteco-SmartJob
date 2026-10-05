using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Repositories;
using VertecoSmartJob.Infrastructure.Persistence.Seed;
using VertecoSmartJob.Tests.Integration;

namespace VertecoSmartJob.Tests.Application;

public class MobileSyncServiceTests
{
    [Fact]
    public async Task Push_ChecklistOperation_AppliesChange()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .Include(j => j.ChecklistItems)
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var technicianId = job.TechnicianId!.Value;
        var checklistItem = job.ChecklistItems.First();
        var newValue = !checklistItem.IsDone;

        var service = CreateService(db);

        var result = await service.PushAsync(
            technicianId,
            [
                new MobileSyncOperationInput
                {
                    OperationId = Guid.NewGuid(),
                    JobCardId = job.Id,
                    OperationType = "SetChecklistItem",
                    RowVersion = Convert.ToBase64String(job.RowVersion),
                    ChecklistItemId = checklistItem.Id,
                    ChecklistItemRowVersion =
                        Convert.ToBase64String(checklistItem.RowVersion),
                    IsDone = newValue
                }
            ]);

        var operationResult = Assert.Single(result.Results);

        Assert.Equal("Applied", operationResult.Status);
        Assert.Equal(job.Id, operationResult.JobCardId);
        Assert.False(string.IsNullOrWhiteSpace(
            operationResult.RowVersion));

        db.ChangeTracker.Clear();

        var savedItem = await db.JobChecklistItems
            .FirstAsync(x => x.Id == checklistItem.Id);

        Assert.Equal(newValue, savedItem.IsDone);

        Assert.Equal(
            1,
            await db.MobileSyncOperations.CountAsync(
                x => x.OperationId == operationResult.OperationId));

        Assert.Equal(
            1,
            await db.AuditLogs.CountAsync(
                x => x.JobCardId == job.Id &&
                     x.Action == "MobileChecklistSynced"));
    }

    [Fact]
    public async Task Push_StaleJobAndSameItemChanged_ReturnsConflict()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .Include(j => j.ChecklistItems)
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var technicianId = job.TechnicianId!.Value;
        var checklistItem = job.ChecklistItems.First();

        var staleJobRowVersion =
            Convert.ToBase64String(job.RowVersion);

        var staleItemRowVersion =
            Convert.ToBase64String(checklistItem.RowVersion);

        var requestedValue = !checklistItem.IsDone;

        // Simulates another server edit to this exact checklist item
        checklistItem.RowVersion = Guid.NewGuid().ToByteArray();
        job.RowVersion = Guid.NewGuid().ToByteArray();

        await db.SaveChangesAsync();

        // Keep the server value different from the mobile request
        checklistItem.IsDone = !requestedValue;
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.PushAsync(
            technicianId,
            [
                new MobileSyncOperationInput
                {
                    OperationId = Guid.NewGuid(),
                    JobCardId = job.Id,
                    OperationType = "SetChecklistItem",
                    RowVersion = staleJobRowVersion,
                    ChecklistItemId = checklistItem.Id,
                    ChecklistItemRowVersion =
                        staleItemRowVersion,
                    IsDone = requestedValue
                }
            ]);

        var operationResult = Assert.Single(result.Results);

        Assert.Equal("Conflict", operationResult.Status);
        Assert.Contains(
            "checklist item changed",
            operationResult.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            1,
            await db.MobileSyncOperations.CountAsync(
                x => x.OperationId == operationResult.OperationId));

        Assert.Equal(
            0,
            await db.AuditLogs.CountAsync(
                x => x.JobCardId == job.Id &&
                     x.Action == "MobileChecklistSynced"));
    }

    [Fact]
    public async Task Push_StaleJobButChecklistItemUnchanged_AutoMerges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .Include(j => j.ChecklistItems)
            .FirstAsync(j =>
                j.Status == JobStatus.InProgress &&
                j.ChecklistItems.Count >= 2);

        var technicianId = job.TechnicianId!.Value;
        var targetItem = job.ChecklistItems.First();
        var otherItem = job.ChecklistItems
            .First(x => x.Id != targetItem.Id);

        var mobileJobRowVersion =
            Convert.ToBase64String(job.RowVersion);

        var mobileItemRowVersion =
            Convert.ToBase64String(targetItem.RowVersion);

        var requestedValue = !targetItem.IsDone;

        // Simulates a server edit to a different checklist item
        otherItem.IsDone = !otherItem.IsDone;
        job.RowVersion = Guid.NewGuid().ToByteArray();

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.PushAsync(
            technicianId,
            [
                new MobileSyncOperationInput
                {
                    OperationId = Guid.NewGuid(),
                    JobCardId = job.Id,
                    OperationType = "SetChecklistItem",
                    RowVersion = mobileJobRowVersion,
                    ChecklistItemId = targetItem.Id,
                    ChecklistItemRowVersion =
                        mobileItemRowVersion,
                    IsDone = requestedValue
                }
            ]);

        var operationResult = Assert.Single(result.Results);

        Assert.Equal("Applied", operationResult.Status);
        Assert.Contains(
            "auto-merged",
            operationResult.Message,
            StringComparison.OrdinalIgnoreCase);

        db.ChangeTracker.Clear();

        var savedTargetItem = await db.JobChecklistItems
            .FirstAsync(x => x.Id == targetItem.Id);

        Assert.Equal(requestedValue, savedTargetItem.IsDone);

        Assert.Equal(
            1,
            await db.AuditLogs.CountAsync(
                x => x.JobCardId == job.Id &&
                     x.Action == "MobileChecklistSynced"));
    }

    [Fact]
    public async Task Push_SameItemAlreadyHasRequestedValue_IsApplied()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .Include(j => j.ChecklistItems)
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var technicianId = job.TechnicianId!.Value;
        var checklistItem = job.ChecklistItems.First();

        var mobileJobRowVersion =
            Convert.ToBase64String(job.RowVersion);

        var mobileItemRowVersion =
            Convert.ToBase64String(checklistItem.RowVersion);

        var requestedValue = !checklistItem.IsDone;

        // Server independently reached the same value as the phone
        checklistItem.IsDone = requestedValue;
        job.RowVersion = Guid.NewGuid().ToByteArray();

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.PushAsync(
            technicianId,
            [
                new MobileSyncOperationInput
                {
                    OperationId = Guid.NewGuid(),
                    JobCardId = job.Id,
                    OperationType = "SetChecklistItem",
                    RowVersion = mobileJobRowVersion,
                    ChecklistItemId = checklistItem.Id,
                    ChecklistItemRowVersion =
                        mobileItemRowVersion,
                    IsDone = requestedValue
                }
            ]);

        var operationResult = Assert.Single(result.Results);

        Assert.Equal("Applied", operationResult.Status);
        Assert.Contains(
            "already has",
            operationResult.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Push_DuplicateOperationId_ReturnsOriginalResult()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .Include(j => j.ChecklistItems)
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var technicianId = job.TechnicianId!.Value;
        var checklistItem = job.ChecklistItems.First();
        var operationId = Guid.NewGuid();

        var service = CreateService(db);

        var request = new MobileSyncOperationInput
        {
            OperationId = operationId,
            JobCardId = job.Id,
            OperationType = "SetChecklistItem",
            RowVersion = Convert.ToBase64String(job.RowVersion),
            ChecklistItemId = checklistItem.Id,
            ChecklistItemRowVersion =
                Convert.ToBase64String(checklistItem.RowVersion),
            IsDone = !checklistItem.IsDone
        };

        var firstPush = await service.PushAsync(
            technicianId,
            [request]);

        var firstResult = Assert.Single(firstPush.Results);

        var secondPush = await service.PushAsync(
            technicianId,
            [request]);

        var secondResult = Assert.Single(secondPush.Results);

        Assert.Equal("Applied", firstResult.Status);
        Assert.Equal(firstResult.Status, secondResult.Status);
        Assert.Equal(firstResult.RowVersion, secondResult.RowVersion);
        Assert.Equal(firstResult.Message, secondResult.Message);

        Assert.Equal(
            1,
            await db.MobileSyncOperations.CountAsync(
                x => x.OperationId == operationId &&
                     x.TechnicianId == technicianId));

        Assert.Equal(
            1,
            await db.AuditLogs.CountAsync(
                x => x.JobCardId == job.Id &&
                     x.Action == "MobileChecklistSynced"));
    }

    [Fact]
    public async Task Push_JobOwnedByAnotherTechnician_ReturnsRejected()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .Include(j => j.ChecklistItems)
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var anotherTechnician = await db.Technicians
            .FirstAsync(t => t.Id != job.TechnicianId);

        var checklistItem = job.ChecklistItems.First();

        var service = CreateService(db);

        var result = await service.PushAsync(
            anotherTechnician.Id,
            [
                new MobileSyncOperationInput
                {
                    OperationId = Guid.NewGuid(),
                    JobCardId = job.Id,
                    OperationType = "SetChecklistItem",
                    RowVersion = Convert.ToBase64String(job.RowVersion),
                    ChecklistItemId = checklistItem.Id,
                    ChecklistItemRowVersion =
                        Convert.ToBase64String(checklistItem.RowVersion),
                    IsDone = !checklistItem.IsDone
                }
            ]);

        var operationResult = Assert.Single(result.Results);

        Assert.Equal("Rejected", operationResult.Status);
        Assert.Contains(
            "not assigned",
            operationResult.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            1,
            await db.MobileSyncOperations.CountAsync(
                x => x.OperationId == operationResult.OperationId));

        Assert.Equal(
            0,
            await db.AuditLogs.CountAsync(
                x => x.JobCardId == job.Id &&
                     x.Action == "MobileChecklistSynced"));
    }

    [Fact]
    public async Task Push_AcceptThenStartInOneOfflineBatch_AppliesInOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards.FirstAsync(j => j.Status == JobStatus.Assigned);
        var technicianId = job.TechnicianId!.Value;
        var originalVersion = Convert.ToBase64String(job.RowVersion);
        var acceptId = Guid.NewGuid();
        var startId = Guid.NewGuid();
        var service = CreateService(db);
        var operations = new[]
        {
            new MobileSyncOperationInput
            {
                OperationId = acceptId, JobCardId = job.Id,
                OperationType = "Accept", RowVersion = originalVersion
            },
            new MobileSyncOperationInput
            {
                OperationId = startId, JobCardId = job.Id,
                OperationType = "Start", RowVersion = originalVersion
            }
        };

        var result = await service.PushAsync(technicianId, operations);
        Assert.All(result.Results, item => Assert.Equal("Applied", item.Status));
        Assert.Equal(JobStatus.InProgress, job.Status);
        Assert.Equal(2, await db.MobileSyncOperations.CountAsync(
            entry => entry.JobCardId == job.Id &&
                     (entry.OperationId == acceptId || entry.OperationId == startId)));

        var retried = await service.PushAsync(technicianId, operations);
        Assert.All(retried.Results, item => Assert.Equal("Applied", item.Status));
        Assert.Equal(2, await db.MobileSyncOperations.CountAsync(
            entry => entry.OperationId == acceptId || entry.OperationId == startId));
    }

    [Fact]
    public async Task Push_CaptureEquipment_AddsItToAssignedJobSite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards.FirstAsync(j => j.Status == JobStatus.InProgress);
        var result = await CreateService(db).PushAsync(job.TechnicianId!.Value,
            [new MobileSyncOperationInput
            {
                OperationId = Guid.NewGuid(), JobCardId = job.Id,
                OperationType = "CaptureEquipment",
                RowVersion = Convert.ToBase64String(job.RowVersion),
                EquipmentName = "Field inverter",
                SerialNumber = "OFFLINE-INV-001"
            }]);

        var applied = Assert.Single(result.Results);
        Assert.Equal("Applied", applied.Status);
        Assert.NotNull(applied.EquipmentId);
        Assert.Equal(job.SiteId, (await db.Equipment.SingleAsync(
            equipment => equipment.SerialNumber == "OFFLINE-INV-001")).SiteId);
    }

    [Fact]
    public async Task Pull_ReportsPreviouslyKnownJobAfterReassignment()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards.FirstAsync(j => j.TechnicianId != null);
        var originalTechnicianId = job.TechnicianId!.Value;
        var otherTechnicianId = await db.Technicians
            .Where(t => t.Id != originalTechnicianId)
            .Select(t => t.Id).FirstAsync();
        var service = CreateService(db);

        var firstPull = await service.PullAsync(originalTechnicianId, null,
            [job.Id]);
        Assert.Contains(firstPull.Jobs, item => item.Id == job.Id);
        Assert.DoesNotContain(job.Id, firstPull.RemovedJobIds);

        db.Entry(job).Property(j => j.TechnicianId).CurrentValue = otherTechnicianId;
        await db.SaveChangesAsync();

        var nextPull = await service.PullAsync(originalTechnicianId,
            firstPull.ServerTimeUtc, [job.Id]);
        Assert.Contains(job.Id, nextPull.RemovedJobIds);
        Assert.DoesNotContain(nextPull.Jobs, item => item.Id == job.Id);
    }

    private static MobileSyncService CreateService(AppDbContext db)
    {
        var jobService = new JobCardService(
            new JobCardRepository(db), new TechnicianRepository(db),
            new JobTemplateRepository(db), new CustomerRepository(db),
            new SiteRepository(db), new AuditLogRepository(db),
            new NoOpNotificationService(),
            new ISubmissionValidationStrategy[]
            {
                new RequiredFieldsStrategy(), new EvidencePresenceStrategy(),
                new SignaturePresenceStrategy()
            }, db);
        return new MobileSyncService(
            new JobCardRepository(db),
            new MobileSyncRepository(db),
            new AuditLogRepository(db),
            db,
            jobService);
    }
}
