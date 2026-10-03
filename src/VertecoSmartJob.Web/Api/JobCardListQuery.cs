namespace VertecoSmartJob.Web.Api;

public class JobCardListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }
    public string? Q { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? TechnicianId { get; set; }

    public bool? Flagged { get; set; }
}