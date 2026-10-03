using SQLite;
using SmartJobMobile.Models;

namespace SmartJobMobile.Services;

public class LocalDatabaseService
{
    private SQLiteAsyncConnection? _database;

    private async Task InitializeAsync()
    {
        if (_database != null)
        {
            return;
        }

        string databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "SmartJob.db3");

        _database = new SQLiteAsyncConnection(
            databasePath);

        await _database.CreateTableAsync<CachedJob>();

        await _database.CreateTableAsync<CachedChecklistItem>();

        await _database.CreateTableAsync<SyncQueueItem>();
    
        }

    // =========================
    // CACHED JOBS
    // =========================

    public async Task<List<CachedJob>> GetJobsAsync()
    {
        await InitializeAsync();

        return await _database!
            .Table<CachedJob>()
            .OrderBy(job => job.ScheduledDate)
            .ToListAsync();
    }

    public async Task<CachedJob?> GetJobAsync(
        string jobId)
    {
        await InitializeAsync();

        return await _database!
            .Table<CachedJob>()
            .Where(job => job.JobId == jobId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveJobAsync(
        CachedJob job)
    {
        await InitializeAsync();

        job.LastUpdated = DateTime.UtcNow;

        await _database!
            .InsertOrReplaceAsync(job);
    }

    public async Task SaveJobsAsync(
        IEnumerable<CachedJob> jobs)
    {
        await InitializeAsync();

        foreach (CachedJob job in jobs)
        {
            job.LastUpdated = DateTime.UtcNow;

            await _database!
                .InsertOrReplaceAsync(job);
        }
    }

    public async Task DeleteJobAsync(
        CachedJob job)
    {
        await InitializeAsync();

        await _database!
            .DeleteAsync(job);
    }

    // =========================
    // SYNC QUEUE
    // =========================

    public async Task<List<SyncQueueItem>>
        GetPendingSyncItemsAsync()
    {
        await InitializeAsync();

        return await _database!
            .Table<SyncQueueItem>()
            .Where(item =>
                item.Status == "Pending")
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();
    }

    public async Task<int>
        GetPendingSyncCountAsync()
    {
        await InitializeAsync();

        return await _database!
            .Table<SyncQueueItem>()
            .Where(item =>
                item.Status == "Pending")
            .CountAsync();
    }

    public async Task AddSyncQueueItemAsync(
        SyncQueueItem item)
    {
        await InitializeAsync();

        if (string.IsNullOrWhiteSpace(
            item.IdempotencyKey))
        {
            item.IdempotencyKey =
                Guid.NewGuid().ToString();
        }

        item.CreatedAt = DateTime.UtcNow;
        item.Status = "Pending";

        await _database!
            .InsertAsync(item);
    }

    public async Task UpdateSyncQueueItemAsync(
        SyncQueueItem item)
    {
        await InitializeAsync();

        await _database!
            .UpdateAsync(item);
    }

    public async Task DeleteSyncQueueItemAsync(
        SyncQueueItem item)
    {
        await InitializeAsync();

        await _database!
            .DeleteAsync(item);
    }

    public async Task ClearCompletedSyncItemsAsync()
    {
        await InitializeAsync();

        List<SyncQueueItem> completedItems =
            await _database!
                .Table<SyncQueueItem>()
                .Where(item =>
                    item.Status == "Completed")
                .ToListAsync();

        foreach (SyncQueueItem item
                 in completedItems)
        {
            await _database.DeleteAsync(item);
        }
    }

    // =========================
    // REMOVE OLD TEST OPERATIONS
    // =========================

    public async Task<int>
        ClearObsoleteTestOperationsAsync()
    {
        await InitializeAsync();

        List<SyncQueueItem> pendingItems =
            await _database!
                .Table<SyncQueueItem>()
                .Where(item =>
                    item.Status == "Pending")
                .ToListAsync();

        List<SyncQueueItem> obsoleteItems =
            pendingItems
                .Where(item =>
                    !item.OperationType.Equals(
                        "setchecklistitem",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (SyncQueueItem item
                 in obsoleteItems)
        {
            await _database!
                .DeleteAsync(item);
        }

        return obsoleteItems.Count;
    }

    // =========================
    // CACHED CHECKLIST ITEMS
    // =========================

    public async Task<List<CachedChecklistItem>>
        GetChecklistItemsAsync(
            string jobId)
    {
        await InitializeAsync();

        return await _database!
            .Table<CachedChecklistItem>()
            .Where(item =>
                item.JobId == jobId)
            .ToListAsync();
    }

    public async Task<CachedChecklistItem?>
        GetChecklistItemAsync(
            string checklistItemId)
    {
        await InitializeAsync();

        return await _database!
            .Table<CachedChecklistItem>()
            .Where(item =>
                item.ChecklistItemId ==
                checklistItemId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveChecklistItemAsync(
        CachedChecklistItem item)
    {
        await InitializeAsync();

        item.LastUpdated =
            DateTime.UtcNow;

        await _database!
            .InsertOrReplaceAsync(item);
    }

    public async Task SaveChecklistItemsAsync(
        IEnumerable<CachedChecklistItem> items)
    {
        await InitializeAsync();

        foreach (CachedChecklistItem item in items)
        {
            item.LastUpdated =
                DateTime.UtcNow;

            await _database!
                .InsertOrReplaceAsync(item);
        }
    }

    // =========================
    // CONFLICTS
    // =========================

    public async Task<List<SyncQueueItem>>
        GetConflictItemsAsync()
    {
        await InitializeAsync();

        return await _database!
            .Table<SyncQueueItem>()
            .Where(item =>
                item.Status == "NeedsReview")
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();
    }

    public async Task<int>
        GetConflictCountAsync()
    {
        await InitializeAsync();

        return await _database!
            .Table<SyncQueueItem>()
            .Where(item =>
                item.Status == "NeedsReview")
            .CountAsync();
    }
}