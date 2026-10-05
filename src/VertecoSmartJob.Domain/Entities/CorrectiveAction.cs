using VertecoSmartJob.Domain.Common;
using VertecoSmartJob.Domain.Exceptions;

namespace VertecoSmartJob.Domain.Entities;

/// <summary>One recorded flag and its eventual resolution for a job card.</summary>
public class CorrectiveAction : Entity
{
    public Guid JobCardId { get; private set; }
    public JobCard? JobCard { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime OpenedAtUtc { get; private set; }
    public Guid? OpenedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public bool IsOpen => ResolvedAtUtc is null;

    private CorrectiveAction() { } // EF Core

    internal static CorrectiveAction Open(Guid jobCardId, string reason, Guid? userId) => new()
    {
        JobCardId = jobCardId,
        Reason = reason.Trim(),
        OpenedAtUtc = DateTime.UtcNow,
        OpenedByUserId = userId
    };

    internal void Resolve(string notes, Guid? userId)
    {
        if (!IsOpen) throw new InvalidFlagStateException("This corrective action is already resolved.");
        if (string.IsNullOrWhiteSpace(notes)) throw new ReasonRequiredException("resolve a flag on");
        ResolutionNotes = notes.Trim();
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedByUserId = userId;
    }
}
