using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Repositories;

// Reads and stores data used by mobile sync
public class MobileSyncRepository(AppDbContext db)
    : IMobileSyncRepository
{
    public async Task<List<JobCard>> GetJobsForTechnicianAsync(
        Guid technicianId,
        DateTime? sinceUtc,
        DateTime serverTimeUtc,
        CancellationToken ct = default)
    {
        var jobs = db.JobCards
            .AsSplitQuery()
            .Include(j => j.ChecklistItems)
            .Where(j => j.TechnicianId == technicianId);

        // Initial sync returns all jobs currently assigned to the technician
        if (sinceUtc is null)
        {
            return await jobs
                .OrderByDescending(j => j.ScheduledDate)
                .ToListAsync(ct);
        }

        // Reconnect sync only returns jobs changed since the last pull
        var changedJobIds = db.MobileSyncChanges
            .Where(change =>
                change.ChangedAtUtc > sinceUtc.Value &&
                change.ChangedAtUtc <= serverTimeUtc)
            .Select(change => change.JobCardId)
            .Distinct();

        return await jobs
            .Where(job => changedJobIds.Contains(job.Id))
            .OrderByDescending(job => job.ScheduledDate)
            .ToListAsync(ct);
    }

    public Task<MobileSyncOperation?> GetOperationAsync(
        Guid technicianId,
        Guid operationId,
        CancellationToken ct = default)
    {
        return db.MobileSyncOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TechnicianId == technicianId &&
                     x.OperationId == operationId,
                ct);
    }

    public async Task AddOperationAsync(
        MobileSyncOperation operation,
        CancellationToken ct = default)
    {
        await db.MobileSyncOperations.AddAsync(
            operation,
            ct);
    }

    public async Task UpdateOperationResultAsync(
        Guid operationId,
        Guid technicianId,
        string result,
        CancellationToken ct = default)
    {
        var operation = await db.MobileSyncOperations
            .FirstOrDefaultAsync(
                x => x.OperationId == operationId &&
                     x.TechnicianId == technicianId,
                ct);

        if (operation is null)
            return;

        operation.Result = result;
    }
}