using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Application.Abstractions;

// Handles data needed by the mobile sync process
public interface IMobileSyncRepository
{
    Task<List<JobCard>> GetJobsForTechnicianAsync(
        Guid technicianId,
        CancellationToken ct = default);

    Task<MobileSyncOperation?> GetOperationAsync(
        Guid technicianId,
        Guid operationId,
        CancellationToken ct = default);

    Task AddOperationAsync(
        MobileSyncOperation operation,
        CancellationToken ct = default);

    Task UpdateOperationResultAsync(
        Guid operationId,
        Guid technicianId,
        string result,
        CancellationToken ct = default);
}