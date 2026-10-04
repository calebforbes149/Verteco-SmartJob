using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Common;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Application.Jobs;

/// <summary>
/// Orchestrates job card use cases. Every write goes through a <see cref="JobCard"/> domain
/// method first, so invalid transitions throw before anything is persisted â€” this class just
/// wires repositories to the aggregate and saves the result.
/// </summary>
public class JobCardService(
    IJobCardRepository jobCards,
    ITechnicianRepository technicians,
    IJobTemplateRepository templates,
    ICustomerRepository customers,
    ISiteRepository sites,
    IAuditLogRepository audit,
    INotificationService notifications,
    IEnumerable<ISubmissionValidationStrategy> submissionStrategies,
    IUnitOfWork unitOfWork,
    ICurrentUser? currentUser = null)
{
    public Task<JobCard?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        jobCards.GetByIdAsync(id, ct);

    public Task<PagedResult<JobCard>> SearchAsync(JobCardQuery query, CancellationToken ct = default) =>
        jobCards.SearchAsync(query, ct);

    public Task<PagedResult<CorrectiveAction>> SearchCorrectiveActionsAsync(CorrectiveActionQuery query, CancellationToken ct = default) =>
        jobCards.SearchCorrectiveActionsAsync(query, ct);

    public Task<CorrectiveAction?> GetCorrectiveActionByIdAsync(Guid id, CancellationToken ct = default) =>
        jobCards.GetCorrectiveActionByIdAsync(id, ct);

    public Task<List<JobCard>> GetDashboardSnapshotAsync(CancellationToken ct = default) =>
        jobCards.GetDashboardSnapshotAsync(ct);

    public async Task<JobCard> CreateAsync(
        Guid customerId, Guid siteId, JobPriority priority,
        DateOnly scheduledDate, string? notes, Guid? technicianId, CancellationToken ct = default)
    {
        var template = (await templates.ListAsync(ct)).SingleOrDefault()
            ?? throw new KeyNotFoundException("Standard job card template not found.");
        var customer = await customers.GetByIdAsync(customerId, ct)
            ?? throw new KeyNotFoundException("Customer not found.");
        var site = await sites.GetByIdAsync(siteId, ct)
            ?? throw new KeyNotFoundException("Site not found.");
        if (site.CustomerId != customer.Id)
            throw new ArgumentException("Site does not belong to the selected customer.");

        var number = await jobCards.NextJobNumberAsync(ct);
        var job = JobCard.Create(number, customerId, siteId, template.Id, priority, scheduledDate, notes,
            template.ChecklistItems.OrderBy(i => i.Order).Select(i => i.Text));

        if (technicianId is { } techId)
        {
            if (await technicians.GetByIdAsync(techId, ct) is null)
                throw new KeyNotFoundException("Technician not found.");
            job.AssignTo(techId);
        }

        template.TimesUsed++;

        await jobCards.AddAsync(job, ct);
        await SaveWithAuditAsync(job, "JobCreated", ct);

        if (technicianId is { } assignedTechId)
            await notifications.NotifyJobAssignedAsync(assignedTechId, job.Number, ct);

        return job;
    }

    public async Task<JobCard> AssignAsync(Guid jobId, Guid technicianId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        var technician = await technicians.GetByIdAsync(technicianId, ct)
            ?? throw new KeyNotFoundException("Technician not found.");

        job.AssignTo(technician.Id);
        await SaveWithAuditAsync(job, "JobAssigned", ct);
        await notifications.NotifyJobAssignedAsync(technician.Id, job.Number, ct);
        return job;
    }

    public async Task<JobCard> UpdateDraftAsync(Guid jobId, JobPriority? priority, DateOnly? scheduledDate, string? notes, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.UpdateDraft(priority, scheduledDate, notes);
        await SaveWithAuditAsync(job, "DraftUpdated", ct);
        return job;
    }

    public async Task<JobCard> AcceptAsync(Guid jobId, Guid technicianId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Accept(technicianId);
        await SaveWithAuditAsync(job, "JobAccepted", ct);
        return job;
    }

    public async Task<JobCard> StartAsync(Guid jobId, Guid technicianId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Start(technicianId);
        await SaveWithAuditAsync(job, "JobStarted", ct);
        return job;
    }

    public async Task<JobCard> ToggleChecklistItemAsync(Guid jobId, Guid checklistItemId, Guid technicianId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.ToggleChecklistItem(checklistItemId, technicianId);
        await SaveWithAuditAsync(job, "ChecklistUpdated", ct);
        return job;
    }

    public async Task<JobCard> SubmitForReviewAsync(Guid jobId, Guid technicianId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        var failures = submissionStrategies.Select(s => s.Validate(job)).Where(e => e is not null).ToArray();
        if (failures.Length > 0)
            throw new Domain.Exceptions.SubmissionInvalidException(string.Join(" ", failures));
        job.SubmitForReview(technicianId);
        await jobCards.AddRevisionAsync(job.Revisions.Last(), ct);
        await SaveWithAuditAsync(job, "JobSubmitted", ct);
        await notifications.NotifyJobSubmittedAsync(job.Number, ct);
        return job;
    }

    public async Task<JobCard> CompleteAsync(Guid jobId, string? reviewNotes, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Complete(reviewNotes);
        await SaveWithAuditAsync(job, "JobApproved", ct);
        if (job.TechnicianId is { } techId)
            await notifications.NotifyJobApprovedAsync(techId, job.Number, ct);
        return job;
    }

    public async Task<JobCard> ReopenAsync(Guid jobId, string reason, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Reopen(reason);
        await SaveWithAuditAsync(job, "JobReopened", ct);
        if (job.TechnicianId is { } techId)
            await notifications.NotifyJobReopenedAsync(techId, job.Number, reason, ct);
        return job;
    }

    public async Task<JobCard> CancelAsync(Guid jobId, string reason, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Cancel(reason);
        await SaveWithAuditAsync(job, "JobCancelled", ct);
        return job;
    }

    public async Task<JobCard> FlagAsync(Guid jobId, string reason, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        var action = job.Flag(reason, currentUser?.UserId);
        await jobCards.AddCorrectiveActionAsync(action, ct);
        await SaveWithAuditAsync(job, "JobFlagged", ct);
        await notifications.NotifyJobFlaggedAsync(job.Number, reason, ct);
        return job;
    }

    public async Task<JobCard> ResolveFlagAsync(Guid jobId, string notes, Guid? actionId = null, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.ResolveFlag(notes, currentUser?.UserId, actionId);
        await SaveWithAuditAsync(job, "FlagResolved", ct);
        return job;
    }

    public async Task<JobCard> ResolveCorrectiveActionAsync(Guid actionId, string notes, CancellationToken ct = default)
    {
        var action = await jobCards.GetCorrectiveActionByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException("Corrective action not found.");
        return await ResolveFlagAsync(action.JobCardId, notes, actionId, ct);
    }

    public async Task<JobCard> AddEvidenceAsync(Guid jobId, Guid technicianId, string reference, string caption, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.AddEvidence(technicianId, reference, caption);
        await jobCards.AddEvidenceAsync(job.Evidence.Last(), ct);
        await SaveWithAuditAsync(job, "EvidenceAdded", ct);
        return job;
    }

    public async Task EnsureFieldEditAllowedAsync(Guid jobId, Guid technicianId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        if (job.Status != JobStatus.InProgress || job.TechnicianId != technicianId)
            throw new Domain.Exceptions.InvalidJobStatusTransitionException(job.Status.ToString(), "Assigned technician field edit");
    }

    // Records equipment found while the technician is working on the job
    public async Task<Equipment> CaptureEquipmentAsync(
        Guid jobId,
        Guid technicianId,
        string name,
        string serialNumber,
        string? description,
        CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);

        if (job.Status != JobStatus.InProgress ||
            job.TechnicianId != technicianId)
        {
            throw new Domain.Exceptions.InvalidJobStatusTransitionException(
                job.Status.ToString(),
                "CaptureEquipment");
        }

        name = name.Trim();
        serialNumber = serialNumber.Trim();

        description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Equipment name is required.");
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Equipment name cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new ArgumentException("Serial number is required.");
        }

        if (serialNumber.Length > 100)
        {
            throw new ArgumentException("Serial number cannot exceed 100 characters.");
        }

        var existing = await sites.FindBySerialNumberAsync(serialNumber, ct);

        if (existing is not null)
        {
            if (existing.SiteId != job.SiteId)
            {
                throw new ArgumentException(
                    "This serial number is already registered to another site.");
            }

            return existing;
        }

        var equipment = new Equipment
        {
            SiteId = job.SiteId,
            Name = name,
            SerialNumber = serialNumber,
            Description = description
        };

        await sites.AddEquipmentAsync(equipment, ct);

        await audit.AddAsync(
            new AuditLog
            {
                ActorUserId = currentUser?.UserId,
                JobCardId = job.Id,
                Action = "EquipmentCaptured",
                Details = serialNumber
            },
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        return equipment;
    }
    public async Task<JobCard> AddSignatureAsync(Guid jobId, Guid technicianId, string signerName, string type, string reference, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.AddSignature(technicianId, signerName, type, reference);
        await jobCards.AddSignatureAsync(job.Signatures.Last(), ct);
        await SaveWithAuditAsync(job, "SignatureAdded", ct);
        return job;
    }

    private async Task SaveWithAuditAsync(JobCard job, string action, CancellationToken ct)
    {
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, JobCardId = job.Id, Action = action, Details = job.Status.ToString() }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<JobCard> RequireJobAsync(Guid jobId, CancellationToken ct) =>
        await jobCards.GetByIdAsync(jobId, ct) ?? throw new KeyNotFoundException($"Job card '{jobId}' was not found.");
}