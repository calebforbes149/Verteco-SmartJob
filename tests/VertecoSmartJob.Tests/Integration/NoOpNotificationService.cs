using VertecoSmartJob.Application.Abstractions;

namespace VertecoSmartJob.Tests.Integration;

/// <summary>A no-op INotificationService for tests that construct JobCardService directly
/// and don't care about actual notification delivery.</summary>
public class NoOpNotificationService : INotificationService
{
    public Task NotifyJobAssignedAsync(Guid technicianId, string jobNumber, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyJobSubmittedAsync(string jobNumber, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyJobApprovedAsync(Guid technicianId, string jobNumber, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyJobReopenedAsync(Guid technicianId, string jobNumber, string reason, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyJobFlaggedAsync(string jobNumber, string reason, CancellationToken ct = default) => Task.CompletedTask;
}