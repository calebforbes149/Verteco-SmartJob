namespace VertecoSmartJob.Contracts.Jobs;

// One job returned to the mobile app during sync
public sealed class MobileSyncJobDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public string? Notes { get; init; }
    public List<MobileSyncChecklistItemDto> ChecklistItems { get; init; } = [];
}

// Checklist data stored by the mobile app
public sealed class MobileSyncChecklistItemDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsDone { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

// Response used when the mobile app pulls server data
public sealed class MobileSyncPullResponse
{
    public DateTime ServerTimeUtc { get; init; }
    public List<MobileSyncJobDto> Jobs { get; init; } = [];
}

// Operations waiting in the mobile offline queue
public sealed class MobileSyncPushRequest
{
    public List<MobileSyncOperationRequest> Operations { get; init; } = [];
}

// One queued operation from the mobile app
public sealed class MobileSyncOperationRequest
{
    public Guid OperationId { get; init; }
    public Guid JobCardId { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public Guid? ChecklistItemId { get; init; }
    public string? ChecklistItemRowVersion { get; init; }
    public bool? IsDone { get; init; }
}

// Results returned after processing the offline queue
public sealed class MobileSyncPushResponse
{
    public List<MobileSyncOperationResult> Results { get; init; } = [];
}

// Result for one queued operation
public sealed class MobileSyncOperationResult
{
    public Guid OperationId { get; init; }
    public Guid JobCardId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RowVersion { get; init; }
    public string? Message { get; init; }
}