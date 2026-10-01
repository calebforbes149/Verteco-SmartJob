using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Domain.Exceptions;

namespace VertecoSmartJob.Tests.Domain;

public class SubmissionRulesTests
{
    private static JobCard InProgress(out Guid technicianId)
    {
        technicianId = Guid.NewGuid();
        var job = JobCard.Create("JC-TEST-SUBMIT", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            JobPriority.High, DateOnly.FromDateTime(DateTime.UtcNow), "Test", ["Inspect equipment"]);
        job.AssignTo(technicianId);
        job.Accept(technicianId);
        job.Start(technicianId);
        job.ToggleChecklistItem(job.ChecklistItems[0].Id, technicianId);
        return job;
    }

    [Fact]
    public void Strategies_RequireEvidenceAndBothSignatures()
    {
        var job = InProgress(out var technicianId);
        Assert.NotNull(new EvidencePresenceStrategy().Validate(job));
        Assert.NotNull(new SignaturePresenceStrategy().Validate(job));
        job.AddEvidence(technicianId, "photo.png:HASH", "Site photo");
        job.AddSignature(technicianId, "Tech", "Technician", "tech.png:HASH");
        Assert.Null(new EvidencePresenceStrategy().Validate(job));
        Assert.NotNull(new SignaturePresenceStrategy().Validate(job));
        job.AddSignature(technicianId, "Client", "Client", "client.png:HASH");
        Assert.Null(new SignaturePresenceStrategy().Validate(job));
    }

    [Fact]
    public void CompletedJob_CanReopenAndCreateLinkedRevision()
    {
        var job = InProgress(out var technicianId);
        job.SubmitForReview(technicianId);
        var first = Assert.Single(job.Revisions);
        job.Complete("Approved");
        job.Reopen("Repeat test");
        job.Start(technicianId);
        job.SubmitForReview(technicianId);
        Assert.Equal(2, job.Revisions.Count);
        Assert.Equal(first.Id, job.Revisions[1].PreviousRevisionId);
        Assert.True(job.Revisions.All(r => r.VerifyIntegrity()));
    }

    [Fact]
    public void Cancellation_RequiresReason()
    {
        var job = InProgress(out _);
        Assert.Throws<ReasonRequiredException>(() => job.Cancel(" "));
        job.Cancel("Client withdrew request");
        Assert.Equal(JobStatus.Cancelled, job.Status);
    }
}
