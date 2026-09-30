using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VertecoSmartJob.Domain.Common;
using VertecoSmartJob.Domain.Enums;
using VertecoSmartJob.Domain.Exceptions;

namespace VertecoSmartJob.Domain.Entities;

/// <summary>
/// Aggregate root for a job card. Every lifecycle move goes through a method here so invalid
/// transitions throw a <see cref="DomainException"/> instead of silently corrupting state —
/// the API layer turns those into 409/422 responses rather than trusting the caller.
/// </summary>
public class JobCard : SoftDeletableEntity
{
    public string Number { get; private set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid SiteId { get; set; }
    public Site? Site { get; set; }

    public Guid JobTemplateId { get; set; }
    public JobTemplate? JobTemplate { get; set; }

    public Guid? TechnicianId { get; private set; }
    public Technician? Technician { get; private set; }

    public JobStatus Status { get; private set; } = JobStatus.Draft;
    public JobPriority Priority { get; set; } = JobPriority.Medium;

    public DateOnly ScheduledDate { get; set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public string? Notes { get; set; }
    public string? ReviewNotes { get; private set; }
    public string? ReopenReason { get; private set; }
    public string? CancelReason { get; private set; }

    /// <summary>SHA-256 hash of the checklist + notes at the moment the technician submitted
    /// for review. Complete() recomputes and compares it so a manager can never sign off on a
    /// job card that quietly changed after submission.</summary>
    public string? SealedHash { get; private set; }
    public DateTime? SealedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public List<JobChecklistItem> ChecklistItems { get; set; } = [];
    public List<JobCardRevision> Revisions { get; set; } = [];
    public List<JobEvidence> Evidence { get; set; } = [];
    public List<JobSignature> Signatures { get; set; } = [];

    private JobCard() { } // EF Core

    public static JobCard Create(string number, Guid customerId, Guid siteId, Guid templateId,
        JobPriority priority, DateOnly scheduledDate, string? notes, IEnumerable<string> checklistTexts)
    {
        var job = new JobCard
        {
            Number = number,
            CustomerId = customerId,
            SiteId = siteId,
            JobTemplateId = templateId,
            Priority = priority,
            ScheduledDate = scheduledDate,
            Notes = notes,
            Status = JobStatus.Draft
        };

        var order = 0;
        foreach (var text in checklistTexts)
        {
            job.ChecklistItems.Add(new JobChecklistItem { Order = order++, Text = text, IsDone = false });
        }

        return job;
    }

    public void AssignTo(Guid technicianId)
    {
        if (Status is not (JobStatus.Draft or JobStatus.Assigned))
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(AssignTo));

        TechnicianId = technicianId;
        Status = JobStatus.Assigned;
    }

    public void UpdateDraft(JobPriority? priority, DateOnly? scheduledDate, string? notes)
    {
        if (Status != JobStatus.Draft)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(UpdateDraft));
        if (priority is { } p) Priority = p;
        if (scheduledDate is { } date) ScheduledDate = date;
        if (notes is not null) Notes = notes;
    }

    public void Accept(Guid technicianId)
    {
        if (Status != JobStatus.Assigned)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(Accept));
        if (TechnicianId != technicianId)
            throw new InvalidJobStatusTransitionException(Status.ToString(), "Accept (wrong technician)");

        Status = JobStatus.Accepted;
    }

    public void Start(Guid technicianId)
    {
        if (Status is not (JobStatus.Accepted or JobStatus.Reopened))
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(Start));
        RequireAssignedTechnician(technicianId);

        Status = JobStatus.InProgress;
    }

    public void ToggleChecklistItem(Guid checklistItemId, Guid technicianId)
    {
        if (Status != JobStatus.InProgress)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(ToggleChecklistItem));
        RequireAssignedTechnician(technicianId);

        var item = ChecklistItems.FirstOrDefault(i => i.Id == checklistItemId)
            ?? throw new InvalidJobStatusTransitionException(Status.ToString(), "ToggleChecklistItem (item not found)");
        item.IsDone = !item.IsDone;
    }

    public void SubmitForReview(Guid technicianId)
    {
        if (Status != JobStatus.InProgress)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(SubmitForReview));
        RequireAssignedTechnician(technicianId);

        var remaining = ChecklistItems.Count(i => !i.IsDone);
        if (remaining > 0)
            throw new ChecklistNotCompleteException(remaining);

        var content = SnapshotContent();
        var previous = Revisions.OrderBy(r => r.Number).LastOrDefault();
        var revision = JobCardRevision.Create(Id, (previous?.Number ?? 0) + 1, content, previous?.Id);
        Revisions.Add(revision);
        Status = JobStatus.AwaitingApproval;
        SealedHash = revision.ContentHash;
        SealedAtUtc = DateTime.UtcNow;
    }

    public void Complete(string? reviewNotes)
    {
        if (Status != JobStatus.AwaitingApproval)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(Complete));

        if (SealedHash is null || ComputeHash() != SealedHash ||
            Revisions.OrderBy(r => r.Number).LastOrDefault()?.VerifyIntegrity() != true)
            throw new JobCardModifiedSinceReviewException();

        ReviewNotes = reviewNotes;
        Status = JobStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Reopen(string reason)
    {
        if (Status is not (JobStatus.AwaitingApproval or JobStatus.Completed))
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(Reopen));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ReasonRequiredException("reopen");

        ReopenReason = reason;
        CompletedAtUtc = null;
        SealedHash = null;
        SealedAtUtc = null;
        Status = JobStatus.Reopened;
    }

    public void Cancel(string reason)
    {
        if (Status is JobStatus.Completed or JobStatus.Cancelled)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(Cancel));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ReasonRequiredException("cancel");

        CancelReason = reason.Trim();
        Status = JobStatus.Cancelled;
    }

    public void AddEvidence(Guid technicianId, string reference, string caption)
    {
        RequireEditable(technicianId);
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Evidence reference is required.");
        Evidence.Add(new JobEvidence { JobCardId = Id, Reference = reference.Trim(), Caption = caption.Trim() });
    }

    public void AddSignature(Guid technicianId, string signerName, string type, string reference)
    {
        RequireEditable(technicianId);
        if (string.IsNullOrWhiteSpace(signerName) || string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Signer name and signature reference are required.");
        if (type is not ("Technician" or "Client")) throw new ArgumentException("Signature type must be Technician or Client.");
        Signatures.Add(new JobSignature { JobCardId = Id, SignerName = signerName.Trim(), Type = type, Reference = reference.Trim() });
    }

    private void RequireEditable(Guid technicianId)
    {
        if (Status != JobStatus.InProgress)
            throw new InvalidJobStatusTransitionException(Status.ToString(), "Edit field work");
        RequireAssignedTechnician(technicianId);
    }

    private void RequireAssignedTechnician(Guid technicianId)
    {
        if (TechnicianId != technicianId)
            throw new InvalidJobStatusTransitionException(Status.ToString(), "Assigned technician required");
    }

    private string SnapshotContent() => JsonSerializer.Serialize(new
    {
        Number, CustomerId, SiteId, JobTemplateId, TechnicianId, Priority, ScheduledDate, Notes,
        Checklist = ChecklistItems.OrderBy(i => i.Order).Select(i => new { i.Order, i.Text, i.IsDone }).ToArray(),
        Evidence = Evidence.OrderBy(e => e.Id).Select(e => new { e.Reference, e.Caption }).ToArray(),
        Signatures = Signatures.OrderBy(s => s.Id).Select(s => new { s.Type, s.SignerName, s.Reference }).ToArray()
    });

    private string ComputeHash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SnapshotContent())));
}

public class JobChecklistItem : Entity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}
