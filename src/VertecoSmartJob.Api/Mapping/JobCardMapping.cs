using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Api.Mapping;

public static class JobCardMapping
{
    public static JobCardSummaryDto ToSummaryDto(this JobCard job) => new()
    {
        Id = job.Id,
        Number = job.Number,
        Status = job.Status.ToString(),
        Priority = job.Priority.ToString(),
        CustomerName = job.Customer?.Name ?? "",
        SiteName = job.Site?.Name ?? "",
        TemplateName = job.JobTemplate?.Name ?? "",
        TechnicianName = job.Technician?.FullName,
        ScheduledDate = job.ScheduledDate
    };

    public static JobCardDetailDto ToDetailDto(this JobCard job) => new()
    {
        Id = job.Id,
        Number = job.Number,
        Status = job.Status.ToString(),
        Priority = job.Priority.ToString(),
        CustomerId = job.CustomerId,
        CustomerName = job.Customer?.Name ?? "",
        SiteId = job.SiteId,
        SiteName = job.Site?.Name ?? "",
        TemplateId = job.JobTemplateId,
        TemplateName = job.JobTemplate?.Name ?? "",
        TemplateCategory = job.JobTemplate?.Category ?? "",
        TechnicianId = job.TechnicianId,
        TechnicianName = job.Technician?.FullName,
        TechnicianSkills = job.Technician?.Skills ?? [],
        ScheduledDate = job.ScheduledDate,
        CompletedAtUtc = job.CompletedAtUtc,
        Notes = job.Notes,
        ReviewNotes = job.ReviewNotes,
        ReopenReason = job.ReopenReason,
        CancelReason = job.CancelReason,
        EvidenceCount = job.Evidence.Count,
        HasTechnicianSignature = job.Signatures.Any(s => s.Type == "Technician"),
        HasClientSignature = job.Signatures.Any(s => s.Type == "Client"),
        Revisions = job.Revisions.OrderBy(r => r.Number).Select(r => new JobRevisionDto
        {
            Number = r.Number, PreviousRevisionId = r.PreviousRevisionId,
            ContentHash = r.ContentHash, SubmittedAtUtc = r.SubmittedAtUtc,
            IntegrityValid = r.VerifyIntegrity()
        }).ToList(),
        ChecklistItems = job.ChecklistItems.OrderBy(i => i.Order).Select(i => new JobChecklistItemDto
        {
            Id = i.Id,
            Order = i.Order,
            Text = i.Text,
            IsDone = i.IsDone
        }).ToList()
    };
}
