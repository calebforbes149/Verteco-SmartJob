using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Domain.Exceptions;

namespace VertecoSmartJob.Tests.Domain;

public sealed class CorrectiveActionTests
{
    [Fact]
    public void FlagAndResolve_RetainTheCorrectiveActionHistory()
    {
        var job = NewJob();
        var actor = Guid.NewGuid();

        var action = job.Flag("Damaged cable", actor);
        Assert.True(job.IsFlagged);
        Assert.Equal("Damaged cable", job.FlagReason);
        Assert.Equal(actor, action.OpenedByUserId);
        Assert.Throws<InvalidFlagStateException>(() => job.Flag("Another issue", actor));

        job.ResolveFlag("Cable replaced and checked", actor, action.Id);
        Assert.False(job.IsFlagged);
        Assert.Null(job.FlagReason);
        Assert.False(action.IsOpen);
        Assert.Equal("Cable replaced and checked", action.ResolutionNotes);
        Assert.Throws<InvalidFlagStateException>(() => job.ResolveFlag("Again", actor));

        var next = job.Flag("Retest required", actor);
        Assert.NotEqual(action.Id, next.Id);
        Assert.Equal(2, job.CorrectiveActions.Count);
    }

    [Fact]
    public void Approval_WaitsForOpenFlagToBeResolved()
    {
        var job = NewJob();
        var technicianId = Guid.NewGuid();
        job.AssignTo(technicianId);
        job.Accept(technicianId);
        job.Start(technicianId);
        foreach (var item in job.ChecklistItems) job.ToggleChecklistItem(item.Id, technicianId);
        job.SubmitForReview(technicianId);

        var action = job.Flag("Client requested another check", Guid.NewGuid());
        Assert.Throws<InvalidFlagStateException>(() => job.Complete("Approved"));

        job.ResolveFlag("Check completed", Guid.NewGuid(), action.Id);
        job.Complete("Approved");
        Assert.Equal(JobStatus.Completed, job.Status);
    }

    private static JobCard NewJob() => JobCard.Create(
        "JC-FLAG-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), JobPriority.Medium,
        DateOnly.FromDateTime(DateTime.UtcNow), "Test", ["Inspect equipment"]);
}
