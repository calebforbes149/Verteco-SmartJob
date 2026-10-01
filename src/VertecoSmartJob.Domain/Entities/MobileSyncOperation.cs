using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

// Keeps track of mobile operations that have already been processed
public class MobileSyncOperation : Entity
{
    public Guid TechnicianId { get; set; }

    public Guid JobCardId { get; set; }

    public Guid OperationId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}