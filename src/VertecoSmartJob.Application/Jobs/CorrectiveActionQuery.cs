namespace VertecoSmartJob.Application.Jobs;

public sealed class CorrectiveActionQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool? IsOpen { get; init; }
    public Guid? JobCardId { get; init; }
    public string? Q { get; init; }
    public string? Sort { get; init; }
}
