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
        CancellationToken ct = default)
    {
        return await db.JobCards
            .AsSplitQuery()
            .Include(j => j.ChecklistItems)
            .Where(j => j.TechnicianId == technicianId)
            .OrderByDescending(j => j.ScheduledDate)
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