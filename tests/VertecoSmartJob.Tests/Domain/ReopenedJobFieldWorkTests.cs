using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Infrastructure.Persistence;

namespace VertecoSmartJob.Tests.Domain;

public sealed class ReopenedJobFieldWorkTests
{
    [Fact]
    public void MigrationGeneratesSqlForRetainedFieldWork()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=SmartJobMigrationCheck;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new AppDbContext(options);
        var script = db.GetService<IMigrator>().GenerateScript(
            "20261004132918_AddRefreshTokens",
            "20261005120000_RetainReopenedFieldWork");

        Assert.Contains("FieldEquipmentName", script);
        Assert.Contains("TechnicianComments", script);
        Assert.Contains("EquipmentCaptured", script);
    }

    [Fact]
    public void ReopenKeepsFieldWorkAndAllowsCorrectionOrRemoval()
    {
        var technicianId = Guid.NewGuid();
        var job = JobCard.Create("JC-REOPEN-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            JobPriority.Medium, DateOnly.FromDateTime(DateTime.UtcNow), null,
            ["Record equipment and serial numbers", "Capture photographic evidence", "Obtain technician and client sign-off"]);
        job.AssignTo(technicianId);
        job.Accept(technicianId);
        job.Start(technicianId);
        job.SetFieldEquipment(technicianId, "Pump", "P-001", "Asset Number: A1");
        job.SetTechnicianComments(technicianId, "Initial test result");
        job.ToggleChecklistItem(job.ChecklistItems[0].Id, technicianId);
        job.AddEvidence(technicianId, "blob:before.jpg:sha256:TEST", "Before repair");
        job.AddSignature(technicianId, "Technician", "Technician", "blob:tech.png:sha256:TEST");
        job.AddSignature(technicianId, "Client", "Client", "blob:client.png:sha256:TEST");
        job.SubmitForReview(technicianId);

        job.Reopen("Correct the test result");
        job.Start(technicianId);

        Assert.Equal("Pump", job.FieldEquipmentName);
        Assert.Equal("P-001", job.FieldEquipmentSerialNumber);
        Assert.Equal("Initial test result", job.TechnicianComments);
        Assert.Single(job.Evidence);
        Assert.Equal(2, job.Signatures.Count);

        job.SetFieldEquipment(technicianId, "Pump", "P-002", "Asset Number: A2");
        job.SetTechnicianComments(technicianId, "Corrected test result");
        var oldEvidence = job.Evidence.Single();
        job.RemoveEvidence(technicianId, oldEvidence.Id);
        Assert.False(job.ChecklistItems[1].IsDone);
        job.AddEvidence(technicianId, "blob:after.jpg:sha256:TEST", "After repair");
        var oldClientSignature = job.Signatures.Single(s => s.Type == "Client");
        job.RemoveSignature(technicianId, oldClientSignature.Id);
        Assert.False(job.ChecklistItems[2].IsDone);
        job.AddSignature(technicianId, "Client", "Client", "blob:new-client.png:sha256:TEST");
        job.SubmitForReview(technicianId);

        Assert.Equal("P-002", job.FieldEquipmentSerialNumber);
        Assert.Equal("Corrected test result", job.TechnicianComments);
        Assert.Equal(2, job.Revisions.Count);
        Assert.All(job.ChecklistItems, item => Assert.True(item.IsDone));
    }
}
