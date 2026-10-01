namespace VertecoSmartJob.Contracts.Jobs;

// Data sent when a technician records equipment on a job
public sealed class MobileEquipmentCaptureRequest
{
    public string Name { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string? Description { get; init; }
}

// Equipment returned after it has been recorded
public sealed class MobileEquipmentResponse
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string? Description { get; init; }
}