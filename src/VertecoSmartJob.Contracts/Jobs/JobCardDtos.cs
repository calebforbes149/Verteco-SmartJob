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
    public string? ReviewNotes { get; set; }
    public string? ReopenReason { get; set; }
    public string? CancelReason { get; set; }
    public List<JobRevisionDto> Revisions { get; set; } = [];
    public int EvidenceCount { get; set; }
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

public class JobRevisionDto
{
    public int Number { get; set; }
    public Guid? PreviousRevisionId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public bool IntegrityValid { get; set; }
}

