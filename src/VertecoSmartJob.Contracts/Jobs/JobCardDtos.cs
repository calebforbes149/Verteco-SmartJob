namespace VertecoSmartJob.Contracts.Jobs;

public class JobCardSummaryDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public bool IsFlagged { get; set; }
    public string? FlagReason { get; set; }
}

public class JobCardDetailDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCategory { get; set; } = string.Empty;

    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public List<string> TechnicianSkills { get; set; } = [];

    public DateOnly ScheduledDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
    public string? FieldEquipmentName { get; set; }
    public string? FieldEquipmentSerialNumber { get; set; }
    public string? FieldEquipmentDescription { get; set; }
    public string? TechnicianComments { get; set; }
    public List<JobSiteEquipmentDto> SiteEquipmentOptions { get; set; } = [];
    public string? ReviewNotes { get; set; }
    public string? ReopenReason { get; set; }
    public string? CancelReason { get; set; }
    public bool IsFlagged { get; set; }
    public string? FlagReason { get; set; }
    public DateTime? FlaggedAtUtc { get; set; }
    public List<CorrectiveActionDto> CorrectiveActions { get; set; } = [];
    public List<JobRevisionDto> Revisions { get; set; } = [];
    public int EvidenceCount { get; set; }
    public List<JobEvidenceDto> Evidence { get; set; } = [];
    public List<JobSignatureDto> Signatures { get; set; } = [];
    public bool HasTechnicianSignature { get; set; }
    public bool HasClientSignature { get; set; }

    public List<JobChecklistItemDto> ChecklistItems { get; set; } = [];
}

public class JobChecklistItemDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}

public class JobSiteEquipmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class JobEvidenceDto
{
    public Guid Id { get; set; }
    public string Caption { get; set; } = string.Empty;
    public bool IsImage { get; set; }
}

public class JobSignatureDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
}

public class CreateJobCardRequest
{
    public Guid CustomerId { get; set; }
    public Guid SiteId { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateOnly ScheduledDate { get; set; }
    public string? Notes { get; set; }
    public Guid? TechnicianId { get; set; }
}

public class AssignTechnicianRequest
{
    public Guid TechnicianId { get; set; }
}

public class PatchJobCardRequest
{
    public string? Priority { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public string? Notes { get; set; }
}

public class CompleteJobRequest
{
    public string? ReviewNotes { get; set; }
}

public class ReopenJobRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CancelJobRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class FlagJobRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateTechnicianCommentsRequest
{
    public string? Comments { get; set; }
}

public class UpdateEvidenceCaptionRequest
{
    public string Caption { get; set; } = string.Empty;
}

public class ResolveFlagRequest
{
    public string ResolutionNotes { get; set; } = string.Empty;
}

public class CorrectiveActionDto
{
    public Guid Id { get; set; }
    public Guid JobCardId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime OpenedAtUtc { get; set; }
    public Guid? OpenedByUserId { get; set; }
    public bool IsOpen { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class JobRevisionDto
{
    public int Number { get; set; }
    public Guid? PreviousRevisionId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public bool IntegrityValid { get; set; }
}

