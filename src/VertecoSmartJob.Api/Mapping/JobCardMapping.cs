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
        ScheduledDate = job.ScheduledDate,
        IsFlagged = job.IsFlagged,
        FlagReason = job.FlagReason
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
        FieldEquipmentName = job.FieldEquipmentName,
        FieldEquipmentSerialNumber = job.FieldEquipmentSerialNumber,
        FieldEquipmentDescription = job.FieldEquipmentDescription,
        TechnicianComments = job.TechnicianComments,
        SiteEquipmentOptions = job.Site?.Equipment.OrderBy(e => e.Name)
            .Select(e => new JobSiteEquipmentDto
            {
                Id = e.Id, Name = e.Name, SerialNumber = e.SerialNumber, Description = e.Description
            }).ToList() ?? [],
        ReviewNotes = job.ReviewNotes,
        ReopenReason = job.ReopenReason,
        CancelReason = job.CancelReason,
        IsFlagged = job.IsFlagged,
        FlagReason = job.FlagReason,
        FlaggedAtUtc = job.FlaggedAtUtc,
        CorrectiveActions = job.CorrectiveActions.OrderByDescending(a => a.OpenedAtUtc)
            .Select(a => a.ToDto(job.Number)).ToList(),
        EvidenceCount = job.Evidence.Count,
        Evidence = job.Evidence.OrderBy(e => e.Id).Select(e => new JobEvidenceDto
        {
            Id = e.Id, Caption = e.Caption, IsImage = IsImageReference(e.Reference)
        }).ToList(),
        Signatures = job.Signatures.OrderBy(s => s.Type).Select(s => new JobSignatureDto
        {
            Id = s.Id, Type = s.Type, SignerName = s.SignerName
        }).ToList(),
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

    private static bool IsImageReference(string reference)
    {
        var name = reference.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
            ? reference.Split(':').ElementAtOrDefault(1) ?? string.Empty
            : reference;
        return Path.GetExtension(name).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png";
    }

    public static CorrectiveActionDto ToDto(this CorrectiveAction action, string? jobNumber = null) => new()
    {
        Id = action.Id,
        JobCardId = action.JobCardId,
        JobNumber = jobNumber ?? action.JobCard?.Number ?? string.Empty,
        Reason = action.Reason,
        OpenedAtUtc = action.OpenedAtUtc,
        OpenedByUserId = action.OpenedByUserId,
        IsOpen = action.IsOpen,
        ResolvedAtUtc = action.ResolvedAtUtc,
        ResolvedByUserId = action.ResolvedByUserId,
        ResolutionNotes = action.ResolutionNotes
    };
}
