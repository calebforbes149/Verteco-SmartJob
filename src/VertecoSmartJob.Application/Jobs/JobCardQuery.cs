using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Application.Jobs;

/// <summary>
/// Backs the API's `page`, `pageSize`, `sort`, `q` and filter query-string convention
/// (spec: every list endpoint supports these) for GET /api/v1/jobcards.
/// </summary>
public class JobCardQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>e.g. "scheduledDate", "-scheduledDate", "number".</summary>
    public string? Sort { get; init; }

    /// <summary>Free-text match against job number, customer name, site name and equipment serial number.</summary>
    public string? Q { get; init; }

    public JobStatus? Status { get; init; }
    public JobPriority? Priority { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? SiteId { get; init; }
    public Guid? TechnicianId { get; init; }
}
