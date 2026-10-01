namespace VertecoSmartJob.Domain.Enums;

/// <summary>
/// Canonical job card lifecycle, decided in Part 2 planning to replace the differing
/// vocabularies used across the Part 1 web and mobile prototypes.
/// </summary>
public enum JobStatus
{
    Draft = 0,
    Assigned = 1,
    Accepted = 2,
    InProgress = 3,
    AwaitingApproval = 4,
    Completed = 5,
    Reopened = 6,
    Cancelled = 7
}
