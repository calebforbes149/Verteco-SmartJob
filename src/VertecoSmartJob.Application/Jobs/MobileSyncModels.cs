namespace VertecoSmartJob.Application.Jobs;

// Job data returned during mobile sync
public sealed class MobileSyncJob
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public string? Notes { get; init; }
    public List<MobileSyncChecklistItem> ChecklistItems { get; init; } = [];
}

// Checklist data returned during mobile sync
public sealed class MobileSyncChecklistItem
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsDone { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

// Data returned when the mobile app pulls jobs
public sealed class MobileSyncPullResult
{
    public DateTime ServerTimeUtc { get; init; }
    public List<MobileSyncJob> Jobs { get; init; } = [];
}

// One operation waiting in the mobile offline queue
public sealed class MobileSyncOperationInput
{
    public Guid OperationId { get; init; }
    public Guid JobCardId { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public Guid? ChecklistItemId { get; init; }
    public string? ChecklistItemRowVersion { get; init; }
    public bool? IsDone { get; init; }
}

// Result of processing the mobile offline queue
public sealed class MobileSyncPushResult
{
    public List<MobileSyncOperationOutput> Results { get; init; } = [];
}

// Result for one mobile sync operation
public sealed class MobileSyncOperationOutput
{
    public Guid OperationId { get; init; }
    public Guid JobCardId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RowVersion { get; init; }
    public string? Message { get; init; }
}