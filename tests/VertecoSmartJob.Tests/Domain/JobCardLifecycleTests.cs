using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Domain.Exceptions;

namespace VertecoSmartJob.Tests.Domain;

public class JobCardLifecycleTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SiteId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid OtherTechnicianId = Guid.NewGuid();

    private static JobCard NewDraftJob(params string[] checklist) =>
        JobCard.Create("JC-TEST-0001", CustomerId, SiteId, TemplateId, JobPriority.Medium,
            DateOnly.FromDateTime(DateTime.Today), "test notes",
            checklist.Length > 0 ? checklist : ["Step one", "Step two"]);

    [Fact]
    public void NewJobCard_StartsInDraft()
    {
        var job = NewDraftJob();
        Assert.Equal(JobStatus.Draft, job.Status);
        Assert.Null(job.TechnicianId);
    }

    [Fact]
    public void FullHappyPath_DraftToCompleted_Succeeds()
    {
        var job = NewDraftJob();

        job.AssignTo(TechnicianId);
        Assert.Equal(JobStatus.Assigned, job.Status);

        job.Accept(TechnicianId);
        Assert.Equal(JobStatus.Accepted, job.Status);

        job.Start(TechnicianId);
        Assert.Equal(JobStatus.InProgress, job.Status);

        foreach (var item in job.ChecklistItems)
            job.ToggleChecklistItem(item.Id, TechnicianId);

        job.SubmitForReview(TechnicianId);
        Assert.Equal(JobStatus.AwaitingApproval, job.Status);
        Assert.NotNull(job.SealedHash);

        job.Complete("Looks good");
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAtUtc);
        Assert.Equal("Looks good", job.ReviewNotes);
    }

    [Fact]
    public void Start_FromDraft_ThrowsInvalidTransition()
    {
        var job = NewDraftJob();
        Assert.Throws<InvalidJobStatusTransitionException>(() => job.Start(TechnicianId));
    }

    [Fact]
    public void Accept_ByWrongTechnician_Throws()
    {
        var job = NewDraftJob();
        job.AssignTo(TechnicianId);

        Assert.Throws<InvalidJobStatusTransitionException>(() => job.Accept(OtherTechnicianId));
    }

    [Fact]
    public void SubmitForReview_WithIncompleteChecklist_ThrowsChecklistNotComplete()
    {
        var job = NewDraftJob("Step one", "Step two", "Step three");
        job.AssignTo(TechnicianId);
        job.Accept(TechnicianId);
        job.Start(TechnicianId);
        job.ToggleChecklistItem(job.ChecklistItems[0].Id, TechnicianId); // only 1 of 3 done

        var ex = Assert.Throws<ChecklistNotCompleteException>(() => job.SubmitForReview(TechnicianId));
        Assert.Equal(2, ex.RemainingItems);
    }

    [Fact]
    public void Complete_AfterChecklistChangedSinceSubmit_ThrowsModifiedSinceReview()
    {
        var job = NewDraftJob();
        job.AssignTo(TechnicianId);
        job.Accept(TechnicianId);
        job.Start(TechnicianId);
        foreach (var item in job.ChecklistItems) job.ToggleChecklistItem(item.Id, TechnicianId);
        job.SubmitForReview(TechnicianId);

        // Simulate the job changing after the seal was taken (e.g. a sync conflict).
        job.Notes = "Changed after submission";

        Assert.Throws<JobCardModifiedSinceReviewException>(() => job.Complete(null));
    }

    [Fact]
    public void Reopen_WithoutReason_ThrowsReasonRequired()
    {
        var job = NewDraftJob();
        job.AssignTo(TechnicianId);
        job.Accept(TechnicianId);
        job.Start(TechnicianId);
        foreach (var item in job.ChecklistItems) job.ToggleChecklistItem(item.Id, TechnicianId);
        job.SubmitForReview(TechnicianId);

        Assert.Throws<ReasonRequiredException>(() => job.Reopen(""));
    }

    [Fact]
    public void Reopen_WithReason_MovesToReopenedAndClearsSeal()
    {
        var job = NewDraftJob();
        job.AssignTo(TechnicianId);
        job.Accept(TechnicianId);
        job.Start(TechnicianId);
        foreach (var item in job.ChecklistItems) job.ToggleChecklistItem(item.Id, TechnicianId);
        job.SubmitForReview(TechnicianId);

        job.Reopen("Needs another load test");

        Assert.Equal(JobStatus.Reopened, job.Status);
        Assert.Equal("Needs another load test", job.ReopenReason);
        Assert.Null(job.SealedHash);
    }

    [Fact]
    public void Reopened_CanStartAgain_WithoutReassigning()
    {
        var job = NewDraftJob();
        job.AssignTo(TechnicianId);
        job.Accept(TechnicianId);
        job.Start(TechnicianId);
        foreach (var item in job.ChecklistItems) job.ToggleChecklistItem(item.Id, TechnicianId);
        job.SubmitForReview(TechnicianId);
        job.Reopen("Redo the checklist");

        job.Start(TechnicianId);

        Assert.Equal(JobStatus.InProgress, job.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var job = NewDraftJob();
        job.Cancel("No longer required"); // Draft -> Cancelled is valid
        Assert.Equal(JobStatus.Cancelled, job.Status);

        Assert.Throws<InvalidJobStatusTransitionException>(() => job.Cancel("No longer required"));
    }

    [Fact]
    public void ToggleChecklistItem_OutsideInProgress_Throws()
    {
        var job = NewDraftJob();
        var itemId = job.ChecklistItems[0].Id;

        Assert.Throws<InvalidJobStatusTransitionException>(() => job.ToggleChecklistItem(itemId, TechnicianId));
    }
}

