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
    public string? FieldEquipmentName { get; private set; }
    public string? FieldEquipmentSerialNumber { get; private set; }
    public string? FieldEquipmentDescription { get; private set; }
    public string? TechnicianComments { get; private set; }
    public string? ReviewNotes { get; private set; }
    public string? ReopenReason { get; private set; }
    public string? CancelReason { get; private set; }

    public bool IsFlagged { get; private set; }
    public string? FlagReason { get; private set; }
    public DateTime? FlaggedAtUtc { get; private set; }
    public List<CorrectiveAction> CorrectiveActions { get; set; } = [];

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

    // Sets a checklist item to the value sent by the mobile app
    public void SetChecklistItem(Guid checklistItemId, bool isDone, Guid technicianId)
    {
        if (Status != JobStatus.InProgress)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(SetChecklistItem));
        RequireAssignedTechnician(technicianId);

        var item = ChecklistItems.FirstOrDefault(i => i.Id == checklistItemId)
            ?? throw new InvalidJobStatusTransitionException(Status.ToString(), "SetChecklistItem (item not found)");
        item.IsDone = isDone;
    }
    public void SubmitForReview(Guid technicianId)
    {
        if (Status != JobStatus.InProgress)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(SubmitForReview));
        RequireAssignedTechnician(technicianId);

        MarkVerifiedWorkflowSteps();

        var remaining = ChecklistItems.Count(i => !i.IsDone);
        if (remaining > 0)
            throw new ChecklistNotCompleteException(remaining);

        var content = SnapshotContent(includeFieldData: true);
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
        if (IsFlagged)
            throw new InvalidFlagStateException("Resolve the open corrective action before approving this job card.");

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

    public CorrectiveAction Flag(string reason, Guid? userId)
    {
        if (Status == JobStatus.Cancelled)
            throw new InvalidJobStatusTransitionException(Status.ToString(), nameof(Flag));
        if (IsFlagged)
            throw new InvalidFlagStateException("This job card already has an open flag.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ReasonRequiredException("flag");

        var action = CorrectiveAction.Open(Id, reason, userId);
        CorrectiveActions.Add(action);
        IsFlagged = true;
        FlagReason = action.Reason;
        FlaggedAtUtc = action.OpenedAtUtc;
        return action;
    }

    public CorrectiveAction ResolveFlag(string notes, Guid? userId, Guid? actionId = null)
    {
        if (!IsFlagged)
            throw new InvalidFlagStateException("This job card has no open flag to resolve.");
        var action = CorrectiveActions.SingleOrDefault(a => a.IsOpen);
        if (action is null || (actionId.HasValue && action.Id != actionId.Value))
            throw new InvalidFlagStateException("The selected corrective action is not the open flag.");

        action.Resolve(notes, userId);
        IsFlagged = false;
        FlagReason = null;
        FlaggedAtUtc = null;
        return action;
    }

    public void AddEvidence(Guid technicianId, string reference, string caption)
    {
        RequireEditable(technicianId);
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Evidence reference is required.");
        Evidence.Add(new JobEvidence { JobCardId = Id, Reference = reference.Trim(), Caption = caption.Trim() });
        MarkVerifiedWorkflowSteps();
    }

    public void SetFieldEquipment(Guid technicianId, string name, string serialNumber, string? description)
    {
        RequireEditable(technicianId);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new ArgumentException("Equipment name must be between 1 and 200 characters.");
        if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber.Length > 100)
            throw new ArgumentException("Serial number must be between 1 and 100 characters.");
        if (description?.Length > 2000)
            throw new ArgumentException("Equipment description cannot exceed 2000 characters.");
        FieldEquipmentName = name.Trim();
        FieldEquipmentSerialNumber = serialNumber.Trim();
        FieldEquipmentDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void ClearFieldEquipment(Guid technicianId)
    {
        RequireEditable(technicianId);
        FieldEquipmentName = null;
        FieldEquipmentSerialNumber = null;
        FieldEquipmentDescription = null;
        foreach (var item in ChecklistItems.Where(i => i.Text.Equals(
                     "Record equipment and serial numbers", StringComparison.OrdinalIgnoreCase)))
            item.IsDone = false;
    }

    public void SetTechnicianComments(Guid technicianId, string? comments)
    {
        RequireEditable(technicianId);
        if (comments?.Length > 2000)
            throw new ArgumentException("Technician comments cannot exceed 2000 characters.");
        TechnicianComments = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
    }

    public void UpdateEvidenceCaption(Guid technicianId, Guid evidenceId, string caption)
    {
        RequireEditable(technicianId);
        var evidence = Evidence.FirstOrDefault(e => e.Id == evidenceId)
            ?? throw new KeyNotFoundException("Evidence not found on this job card.");
        if (caption is null || caption.Length > 500)
            throw new ArgumentException("Evidence caption cannot exceed 500 characters.");
        evidence.Caption = caption.Trim();
    }

    public JobEvidence RemoveEvidence(Guid technicianId, Guid evidenceId)
    {
        RequireEditable(technicianId);
        var evidence = Evidence.FirstOrDefault(e => e.Id == evidenceId)
            ?? throw new KeyNotFoundException("Evidence not found on this job card.");
        Evidence.Remove(evidence);
        MarkVerifiedWorkflowSteps();
        return evidence;
    }

    public JobSignature RemoveSignature(Guid technicianId, Guid signatureId)
    {
        RequireEditable(technicianId);
        var signature = Signatures.FirstOrDefault(s => s.Id == signatureId)
            ?? throw new KeyNotFoundException("Signature not found on this job card.");
        Signatures.Remove(signature);
        MarkVerifiedWorkflowSteps();
        return signature;
    }

    public void AddSignature(Guid technicianId, string signerName, string type, string reference)
    {
        RequireEditable(technicianId);
        if (string.IsNullOrWhiteSpace(signerName) || string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Signer name and signature reference are required.");
        if (type is not ("Technician" or "Client")) throw new ArgumentException("Signature type must be Technician or Client.");
        Signatures.Add(new JobSignature { JobCardId = Id, SignerName = signerName.Trim(), Type = type, Reference = reference.Trim() });
        MarkVerifiedWorkflowSteps();
    }

    public void MarkVerifiedWorkflowSteps()
    {
        foreach (var item in ChecklistItems)
        {
            if (item.Text.Equals("Capture photographic evidence", StringComparison.OrdinalIgnoreCase))
                item.IsDone = Evidence.Count > 0;
            else if (item.Text.Equals("Obtain technician and client sign-off", StringComparison.OrdinalIgnoreCase))
                item.IsDone = Signatures.Any(s => s.Type == "Technician") &&
                              Signatures.Any(s => s.Type == "Client");
        }
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

    private string SnapshotContent(bool includeFieldData)
    {
        var checklist = ChecklistItems.OrderBy(i => i.Order).Select(i => new { i.Order, i.Text, i.IsDone }).ToArray();
        var evidence = Evidence.OrderBy(e => e.Id).Select(e => new { e.Reference, e.Caption }).ToArray();
        var signatures = Signatures.OrderBy(s => s.Id).Select(s => new { s.Type, s.SignerName, s.Reference }).ToArray();
        if (!includeFieldData)
            return JsonSerializer.Serialize(new
            {
                Number, CustomerId, SiteId, JobTemplateId, TechnicianId, Priority, ScheduledDate, Notes,
                Checklist = checklist, Evidence = evidence, Signatures = signatures
            });
        return JsonSerializer.Serialize(new
        {
            Number, CustomerId, SiteId, JobTemplateId, TechnicianId, Priority, ScheduledDate, Notes,
            Checklist = checklist, Evidence = evidence, Signatures = signatures,
            FieldEquipmentName, FieldEquipmentSerialNumber, FieldEquipmentDescription, TechnicianComments
        });
    }

    private string ComputeHash()
    {
        var latest = Revisions.OrderBy(r => r.Number).LastOrDefault();
        var includesFieldData = latest?.ContentJson.Contains("\"FieldEquipmentName\"", StringComparison.Ordinal) == true;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SnapshotContent(includesFieldData))));
    }
}

public class JobChecklistItem : Entity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}
