using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Repositories;
using VertecoSmartJob.Infrastructure.Persistence.Seed;

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

    private static MobileSyncService CreateService(AppDbContext db)
    {
        return new MobileSyncService(
            new JobCardRepository(db),
            new MobileSyncRepository(db),
            new AuditLogRepository(db),
            db);
    }
}