using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Repositories;
using VertecoSmartJob.Infrastructure.Persistence.Seed;
using VertecoSmartJob.Tests.Integration;

namespace VertecoSmartJob.Tests.Application;

public class MobileEquipmentServiceTests
{
    [Fact]
    public async Task CaptureEquipment_InProgressJob_AddsEquipmentToJobSite()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var technicianId = job.TechnicianId!.Value;

        var service = CreateService(db);

        var equipment = await service.CaptureEquipmentAsync(
            job.Id,
            technicianId,
            "Mobile Test Equipment",
            "MOBILE-SERIAL-001",
            "Captured during mobile job");

        Assert.Equal(job.SiteId, equipment.SiteId);
        Assert.Equal("Mobile Test Equipment", equipment.Name);
        Assert.Equal("MOBILE-SERIAL-001", equipment.SerialNumber);
        Assert.Equal(
            "Captured during mobile job",
            equipment.Description);

        db.ChangeTracker.Clear();

        var savedEquipment = await db.Equipment
            .SingleAsync(
                e => e.SerialNumber == "MOBILE-SERIAL-001");

        Assert.Equal(job.SiteId, savedEquipment.SiteId);

        Assert.Equal(
            1,
            await db.AuditLogs.CountAsync(
                x => x.JobCardId == job.Id &&
                     x.Action == "EquipmentCaptured"));
    }

    [Fact]
    public async Task CaptureEquipment_ExistingSerialAtSameSite_ReturnsExisting()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var technicianId = job.TechnicianId!.Value;

        var existing = await db.Equipment
            .FirstOrDefaultAsync(e => e.SiteId == job.SiteId);

        if (existing is null)
        {
            existing = new Equipment
            {
                SiteId = job.SiteId,
                Name = "Existing Equipment",
                SerialNumber = "EXISTING-SAME-SITE-001",
                Description = "Existing test equipment"
            };

            await db.Equipment.AddAsync(existing);
            await db.SaveChangesAsync();
        }

        var originalCount = await db.Equipment.CountAsync();

        var service = CreateService(db);

        var result = await service.CaptureEquipmentAsync(
            job.Id,
            technicianId,
            "Different Name",
            existing.SerialNumber,
            "Different description");

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.SerialNumber, result.SerialNumber);

        Assert.Equal(
            originalCount,
            await db.Equipment.CountAsync());
    }

    [Fact]
    public async Task CaptureEquipment_SerialRegisteredAtAnotherSite_Throws()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var technicianId = job.TechnicianId!.Value;

        var otherSite = await db.Sites
            .FirstAsync(s => s.Id != job.SiteId);

        var equipment = new Equipment
        {
            SiteId = otherSite.Id,
            Name = "Other Site Equipment",
            SerialNumber = "OTHER-SITE-SERIAL-001",
            Description = "Belongs to another site"
        };

        await db.Equipment.AddAsync(equipment);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CaptureEquipmentAsync(
                job.Id,
                technicianId,
                "Mobile Equipment",
                "OTHER-SITE-SERIAL-001",
                null));

        Assert.Contains(
            "another site",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CaptureEquipment_OtherTechnician_IsRejected()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);

        var job = await db.JobCards
            .FirstAsync(j => j.Status == JobStatus.InProgress);

        var anotherTechnician = await db.Technicians
            .FirstAsync(t => t.Id != job.TechnicianId);

        var service = CreateService(db);

        await Assert.ThrowsAsync<
            VertecoSmartJob.Domain.Exceptions.InvalidJobStatusTransitionException>(
            () => service.CaptureEquipmentAsync(
                job.Id,
                anotherTechnician.Id,
                "Unauthorized Equipment",
                "UNAUTHORIZED-SERIAL-001",
                null));

        Assert.Equal(
            0,
            await db.Equipment.CountAsync(
                e => e.SerialNumber ==
                     "UNAUTHORIZED-SERIAL-001"));
    }

    private static JobCardService CreateService(AppDbContext db)
    {
        return new JobCardService(
            new JobCardRepository(db),
            new TechnicianRepository(db),
            new JobTemplateRepository(db),
            new CustomerRepository(db),
            new SiteRepository(db),
            new AuditLogRepository(db),
            new NoOpNotificationService(),
            Array.Empty<ISubmissionValidationStrategy>(),
            db);
    }
}
