namespace VertecoSmartJob.Application.Abstractions;

public interface INotificationService
{
    Task NotifyJobAssignedAsync(Guid technicianId, string jobNumber, CancellationToken ct = default);
    Task NotifyJobSubmittedAsync(string jobNumber, CancellationToken ct = default);
    Task NotifyJobApprovedAsync(Guid technicianId, string jobNumber, CancellationToken ct = default);
    Task NotifyJobReopenedAsync(Guid technicianId, string jobNumber, string reason, CancellationToken ct = default);
    Task NotifyJobFlaggedAsync(string jobNumber, string reason, CancellationToken ct = default);
}