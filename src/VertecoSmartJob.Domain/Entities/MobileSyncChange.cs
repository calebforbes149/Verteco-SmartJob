using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

// Records job changes that mobile devices need to pull after reconnecting
public class MobileSyncChange : Entity
{
    public Guid JobCardId { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
