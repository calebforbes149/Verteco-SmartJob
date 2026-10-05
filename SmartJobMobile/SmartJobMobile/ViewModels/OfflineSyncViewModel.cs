using SmartJobMobile.Models;
using SmartJobMobile.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class OfflineSyncViewModel : INotifyPropertyChanged
{
    private readonly ConnectivityService _connectivityService;
    private readonly LocalDatabaseService _localDatabaseService;
    private readonly ApiService _apiService;

    private string _connectionStatus = string.Empty;
    private string _connectionMessage = string.Empty;
    private string _connectionColor = "#EF5350";
    private string _connectionIcon = "●";

    private string _syncStatus = "Up to date";
    private string _syncStatusColor = "#66BB6A";
    private string _lastSync = "Not synced yet";

    private int _pendingOperations;
    private int _pendingJobUpdates;
    private int _pendingEvidence;
    private int _conflictCount;

    private bool _isSyncing;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    // =========================================================
    // CONNECTION STATUS
    // =========================================================

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            _connectionStatus = value;
            OnPropertyChanged();
        }
    }

    public string ConnectionMessage
    {
        get => _connectionMessage;
        set
        {
            _connectionMessage = value;
            OnPropertyChanged();
        }
    }

    public string ConnectionColor
    {
        get => _connectionColor;
        set
        {
            _connectionColor = value;
            OnPropertyChanged();
        }
    }

    public string ConnectionIcon
    {
        get => _connectionIcon;
        set
        {
            _connectionIcon = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // SYNC STATUS
    // =========================================================

    public string SyncStatus
    {
        get => _syncStatus;
        set
        {
            _syncStatus = value;
            OnPropertyChanged();
        }
    }

    public string SyncStatusColor
    {
        get => _syncStatusColor;
        set
        {
            _syncStatusColor = value;
            OnPropertyChanged();
        }
    }

    public string LastSync
    {
        get => _lastSync;
        set
        {
            _lastSync = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // QUEUE COUNTS
    // =========================================================

    public int PendingOperations
    {
        get => _pendingOperations;
        set
        {
            _pendingOperations = value;
            OnPropertyChanged();
        }
    }

    public int PendingJobUpdates
    {
        get => _pendingJobUpdates;
        set
        {
            _pendingJobUpdates = value;
            OnPropertyChanged();
        }
    }

    public int PendingEvidence
    {
        get => _pendingEvidence;
        set
        {
            _pendingEvidence = value;
            OnPropertyChanged();
        }
    }

    public int ConflictCount
    {
        get => _conflictCount;
        set
        {
            _conflictCount = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // SYNC STATE
    // =========================================================

    public bool IsSyncing
    {
        get => _isSyncing;
        set
        {
            _isSyncing = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSync));
            OnPropertyChanged(nameof(SyncButtonText));
        }
    }

    public bool CanSync =>
        !IsSyncing &&
        _connectivityService.IsOnline;

    public string SyncButtonText =>
        IsSyncing
            ? "SYNCING..."
            : "SYNC NOW";

    // =========================================================
    // ERROR STATE
    // =========================================================

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // COMMANDS
    // =========================================================

    public ICommand SyncCommand { get; }

    public ICommand RefreshCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public OfflineSyncViewModel(
        ConnectivityService connectivityService,
        LocalDatabaseService localDatabaseService,
        ApiService apiService)
    {
        _connectivityService =
            connectivityService;

        _localDatabaseService =
            localDatabaseService;

        _apiService =
            apiService;

        SyncCommand =
            new Command(
                async () => await SyncAsync());

        RefreshCommand =
            new Command(
                async () => await RefreshStatusAsync());

        _connectivityService.ConnectivityChanged +=
            OnConnectivityChanged;

        _ = LoadInitialDataAsync();
    }

    // =========================================================
    // INITIAL LOAD
    // =========================================================

    private async Task LoadInitialDataAsync()
    {
        try
        {
            UpdateConnectionStatus();

            await LoadQueueInformationAsync();

            if (_connectivityService.IsOnline)
            {
                SyncStatus =
                    PendingOperations > 0
                        ? "Ready to sync"
                        : "Up to date";

                SyncStatusColor = "#66BB6A";
            }
            else
            {
                SyncStatus =
                    "Waiting for connection";

                SyncStatusColor =
                    "#FFC107";
            }
        }
        catch (Exception)
        {
            ShowError(
                "The local SmartJob database could not be loaded.");

            SyncStatus =
                "Local database error";

            SyncStatusColor =
                "#EF5350";
        }
    }

    // =========================================================
    // LOAD SQLITE QUEUE INFORMATION
    // =========================================================

    private async Task LoadQueueInformationAsync()
    {
        List<SyncQueueItem> pendingItems =
            await _localDatabaseService
                .GetPendingSyncItemsAsync();

        PendingOperations =
            pendingItems.Count;

        PendingEvidence =
            pendingItems.Count(item =>
                item.OperationType ==
                "UploadEvidence");

        PendingJobUpdates =
            pendingItems.Count(item =>
                item.OperationType !=
                "UploadEvidence");

        ConflictCount =
            await _localDatabaseService
                .GetConflictCountAsync();
    }

    // =========================================================
    // CONNECTION STATUS
    // =========================================================

    private void UpdateConnectionStatus()
    {
        ConnectionStatus =
            _connectivityService.ConnectionStatus;

        ConnectionMessage =
            _connectivityService.ConnectionMessage;

        ConnectionColor =
            _connectivityService.ConnectionColor;

        ConnectionIcon = "●";

        OnPropertyChanged(nameof(CanSync));
    }

    private void OnConnectivityChanged(
        object? sender,
        bool isOnline)
    {
        MainThread.BeginInvokeOnMainThread(
            async () =>
            {
                UpdateConnectionStatus();

                await LoadQueueInformationAsync();

                if (isOnline)
                {
                    SyncStatus =
                        PendingOperations > 0
                            ? "Ready to sync"
                            : "Up to date";

                    SyncStatusColor =
                        "#66BB6A";
                }
                else
                {
                    SyncStatus =
                        "Waiting for connection";

                    SyncStatusColor =
                        "#FFC107";
                }
            });
    }

    // =========================================================
    // SYNC NOW
    // =========================================================

    private async Task SyncAsync()
    {
        ClearError();

        try
        {
            // =====================================================
            // CHECK CONNECTION
            // =====================================================

            UpdateConnectionStatus();

            if (!_connectivityService.IsOnline)
            {
                await Shell.Current.DisplayAlert(
                    "Offline",
                    "SmartJob cannot synchronise while the device is offline.",
                    "OK");

                return;
            }

            IsSyncing = true;
            SyncStatus = "Syncing...";

            // =====================================================
            // GET PENDING OPERATIONS
            // =====================================================

            List<SyncQueueItem> pendingItems =
                await _localDatabaseService
                    .GetPendingSyncItemsAsync();

            // Only checklist updates are currently supported
            // by the mobile sync API.

            List<SyncQueueItem> supportedItems =
                pendingItems
                    .Where(item =>
                        item.OperationType.Equals(
                            "setchecklistitem",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // =====================================================
            // BUILD PUSH REQUEST
            // =====================================================

            MobileSyncPushRequest request =
                new MobileSyncPushRequest();

            Dictionary<Guid, SyncQueueItem>
                queueItemsByOperationId =
                    new Dictionary<Guid, SyncQueueItem>();

            foreach (SyncQueueItem queueItem
                     in supportedItems)
            {
                // ---------------------------------------------
                // Validate operation ID
                // ---------------------------------------------

                if (!Guid.TryParse(
                        queueItem.IdempotencyKey,
                        out Guid operationId))
                {
                    queueItem.Status =
                        "Rejected";

                    queueItem.LastError =
                        "The queue item has an invalid idempotency key.";

                    queueItem.RetryCount++;

                    await _localDatabaseService
                        .UpdateSyncQueueItemAsync(
                            queueItem);

                    continue;
                }

                // ---------------------------------------------
                // Validate job ID
                // ---------------------------------------------

                if (!Guid.TryParse(
                        queueItem.JobId,
                        out Guid jobCardId))
                {
                    queueItem.Status =
                        "Rejected";

                    queueItem.LastError =
                        "The queue item has an invalid job ID.";

                    queueItem.RetryCount++;

                    await _localDatabaseService
                        .UpdateSyncQueueItemAsync(
                            queueItem);

                    continue;
                }

                // ---------------------------------------------
                // Read payload
                // ---------------------------------------------

                ChecklistSyncPayload?
                    payload =
                        JsonSerializer
                            .Deserialize<ChecklistSyncPayload>(
                                queueItem.PayloadJson,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive =
                                        true
                                });

                if (payload == null ||
                    payload.ChecklistItemId ==
                        Guid.Empty ||
                    string.IsNullOrWhiteSpace(
                        payload.ChecklistItemRowVersion))
                {
                    queueItem.Status =
                        "Rejected";

                    queueItem.LastError =
                        "The checklist sync payload is incomplete.";

                    queueItem.RetryCount++;

                    await _localDatabaseService
                        .UpdateSyncQueueItemAsync(
                            queueItem);

                    continue;
                }

                // ---------------------------------------------
                // Create API operation
                // ---------------------------------------------

                MobileSyncOperationRequest
                    operation =
                        new MobileSyncOperationRequest
                        {
                            OperationId =
                                operationId,

                            JobCardId =
                                jobCardId,

                            OperationType =
                                "setchecklistitem",

                            RowVersion =
                                queueItem.BaseRowVersion,

                            ChecklistItemId =
                                payload.ChecklistItemId,

                            ChecklistItemRowVersion =
                                payload.ChecklistItemRowVersion,

                            IsDone =
                                payload.IsDone
                        };

                request.Operations.Add(
                    operation);

                queueItemsByOperationId[
                    operationId] =
                    queueItem;
            }

            // =====================================================
            // NOTHING VALID TO SEND
            // =====================================================

            // =====================================================
            // SEND TO REAL API
            // =====================================================

            MobileSyncPushResponse? response =
                request.Operations.Count == 0
                    ? new MobileSyncPushResponse()
                    : await _apiService.PushSyncDataAsync(request);

            if (response == null)
            {
                throw new Exception(
                    "The sync API returned no response.");
            }

            int appliedCount = 0;
            int conflictCount = 0;
            int rejectedCount = 0;

            // =====================================================
            // PROCESS SERVER RESULTS
            // =====================================================

            foreach (
                MobileSyncOperationResult result
                in response.Results)
            {
                if (!queueItemsByOperationId
                        .TryGetValue(
                            result.OperationId,
                            out SyncQueueItem? queueItem))
                {
                    continue;
                }

                string resultStatus =
                    result.Status?.Trim()
                    ?? string.Empty;

                // ---------------------------------------------
                // APPLIED
                // ---------------------------------------------

                if (resultStatus.Equals(
                    "Applied",
                    StringComparison.OrdinalIgnoreCase))
                {
                    queueItem.Status =
                        "Completed";

                    queueItem.LastError =
                        string.Empty;

                    await _localDatabaseService
                        .UpdateSyncQueueItemAsync(
                            queueItem);

                    appliedCount++;

                    continue;
                }

                // ---------------------------------------------
                // CONFLICT
                // ---------------------------------------------

                if (resultStatus.Equals(
                    "Conflict",
                    StringComparison.OrdinalIgnoreCase))
                {
                    queueItem.Status =
                        "NeedsReview";

                    queueItem.LastError =
                        result.Message
                        ?? "The server version changed while this device was offline.";

                    queueItem.RetryCount++;

                    await _localDatabaseService
                        .UpdateSyncQueueItemAsync(
                            queueItem);

                    conflictCount++;

                    continue;
                }

                // ---------------------------------------------
                // REJECTED / UNKNOWN RESULT
                // ---------------------------------------------

                queueItem.Status =
                    "Rejected";

                queueItem.LastError =
                    result.Message
                    ?? "The server rejected this operation.";

                queueItem.RetryCount++;

                await _localDatabaseService
                    .UpdateSyncQueueItemAsync(
                        queueItem);

                rejectedCount++;
            }

            // =====================================================
            // REMOVE SUCCESSFUL QUEUE ITEMS
            // =====================================================

            await _localDatabaseService
                .ClearCompletedSyncItemsAsync();

            // =====================================================
            // PULL FRESH SERVER STATE
            // =====================================================

            MobileSyncPullResponse? pullResponse =
                await PullAssignedJobsAsync();

            if (pullResponse is null)
                throw new InvalidOperationException("The sync API returned no jobs response.");

            if (pullResponse != null)
            {
                foreach (
                    MobileSyncJob serverJob
                    in pullResponse.Jobs)
                {
                    CachedJob cachedJob =
                        new CachedJob
                        {
                            JobId =
                                serverJob.Id.ToString(),

                            JobNumber =
                                serverJob.Number,

                            Title =
                                "Job Card",

                            Status =
                                serverJob.Status,

                            ScheduledDate =
                                serverJob.ScheduledDate
                                    .ToDateTime(
                                        TimeOnly.MinValue),

                            Description =
                                serverJob.Notes
                                ?? string.Empty,

                            RowVersion =
                                serverJob.RowVersion,

                            LastUpdated =
                                DateTime.UtcNow,

                            HasLocalChanges =
                                false
                        };

                    await _localDatabaseService
                        .SaveJobAsync(
                            cachedJob);

                    List<CachedChecklistItem>
                        cachedChecklistItems =
                            serverJob.ChecklistItems
                                .Select(
                                    item =>
                                        new CachedChecklistItem
                                        {
                                            ChecklistItemId =
                                                item.Id.ToString(),

                                            JobId =
                                                serverJob.Id.ToString(),

                                            Text =
                                                item.Text,

                                            IsDone =
                                                item.IsDone,

                                            RowVersion =
                                                item.RowVersion,

                                            LastUpdated =
                                                DateTime.UtcNow
                                        })
                                .ToList();

                    await _localDatabaseService
                        .SaveChecklistItemsAsync(
                            cachedChecklistItems);
                }

                await RemoveUnassignedJobsAsync(pullResponse.RemovedJobIds);
            }

            // =====================================================
            // REFRESH SCREEN COUNTS
            // =====================================================

            await LoadQueueInformationAsync();

            LastSync =
                DateTime.Now.ToString(
                    "dd MMM yyyy HH:mm");

            if (conflictCount > 0)
            {
                SyncStatus =
                    "Needs review";
            }
            else if (PendingOperations > 0)
            {
                SyncStatus =
                    "Pending operations";
            }
            else
            {
                SyncStatus =
                    "Up to date";
            }

            // =====================================================
            // RESULT MESSAGE
            // =====================================================

            await Shell.Current.DisplayAlert(
                "Sync Complete",
                $"Applied: {appliedCount}\n" +
                $"Needs Review: {conflictCount}\n" +
                $"Rejected: {rejectedCount}",
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Sync failed: {ex}");

            SyncStatus =
                "Sync failed";

            ShowError(
                "SmartJob could not synchronise with the server.");

            await Shell.Current.DisplayAlert(
                "Sync Failed",
                ex.Message,
                "OK");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    // =========================================================
    // REFRESH / SYNC PULL
    // =========================================================

    private async Task RefreshStatusAsync()
    {
        try
        {
            ClearError();

            UpdateConnectionStatus();

            await LoadQueueInformationAsync();

            // -------------------------------------------------
            // CHECK CONNECTION
            // -------------------------------------------------

            if (!_connectivityService.IsOnline)
            {
                SyncStatus =
                    "Waiting for connection";

                SyncStatusColor =
                    "#FFC107";

                await Shell.Current.DisplayAlert(
                    "Offline",
                    "SmartJob currently detects this device as offline.",
                    "OK");

                return;
            }

            // -------------------------------------------------
            // CALL REAL PERSON 4 SYNC PULL API
            // -------------------------------------------------

            SyncStatus =
                "Checking server";

            SyncStatusColor =
                "#FFC107";

            MobileSyncPullResponse? response =
                await PullAssignedJobsAsync();

            // -------------------------------------------------
            // CHECK SERVER RESPONSE
            // -------------------------------------------------

            if (response == null)
            {
                SyncStatus =
                    "Server unavailable";

                SyncStatusColor =
                    "#EF5350";

                await Shell.Current.DisplayAlert(
                    "Sync Pull Failed",
                    "The server returned no sync information.",
                    "OK");

                return;
            }

            // -------------------------------------------------
            // SAVE SERVER JOBS TO SQLITE
            // -------------------------------------------------

            foreach (
                MobileSyncJob serverJob
                in response.Jobs)
            {
                CachedJob cachedJob =
                    new CachedJob
                    {
                        JobId =
                            serverJob.Id
                                .ToString(),

                        JobNumber =
                            serverJob.Number,

                        Title =
                            "Job Card",

                        Status =
                            serverJob.Status,

                        ScheduledDate =
                            serverJob
                                .ScheduledDate
                                .ToDateTime(
                                    TimeOnly.MinValue),

                        RowVersion =
                            serverJob.RowVersion,

                        Description =
                            serverJob.Notes
                            ?? string.Empty,

                        HasLocalChanges =
                            false
                    };

                await _localDatabaseService
                    .SaveJobAsync(
                        cachedJob);

                // ---------------------------------------------
                // SAVE CHECKLIST ITEMS + ROWVERSIONS
                // ---------------------------------------------

                List<CachedChecklistItem>
                    cachedChecklistItems =
                        serverJob
                            .ChecklistItems
                            .Select(
                                serverItem =>
                                    new CachedChecklistItem
                                    {
                                        ChecklistItemId =
                                            serverItem.Id
                                                .ToString(),

                                        JobId =
                                            serverJob.Id
                                                .ToString(),

                                        Text =
                                            serverItem.Text,

                                        IsDone =
                                            serverItem.IsDone,

                                        RowVersion =
                                            serverItem.RowVersion
                                    })
                            .ToList();

                await _localDatabaseService
                    .SaveChecklistItemsAsync(
                        cachedChecklistItems);
            }

            await RemoveUnassignedJobsAsync(response.RemovedJobIds);

            // -------------------------------------------------
            // UPDATE UI AFTER SUCCESSFUL CACHE
            // -------------------------------------------------

            await LoadQueueInformationAsync();

            SyncStatus =
                PendingOperations > 0
                    ? "Ready to sync"
                    : "Up to date";

            SyncStatusColor =
                "#66BB6A";

            LastSync =
                DateTime.Now.ToString("HH:mm");

            await Shell.Current.DisplayAlert(
                "Sync Pull Successful",
                $"{response.Jobs.Count} job(s) received and saved to this device.",
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Sync pull failed: {ex}");

            SyncStatus =
                "Server sync failed";

            SyncStatusColor =
                "#EF5350";

            ShowError(
                "SmartJob could not retrieve sync data from the server.");

            await Shell.Current.DisplayAlert(
                "Sync Pull Error",
                ex.Message,
                "OK");
        }
    }

    private async Task<MobileSyncPullResponse?> PullAssignedJobsAsync()
    {
        var knownJobIds = (await _localDatabaseService.GetJobsAsync())
            .Select(job => Guid.TryParse(job.JobId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();
        return await _apiService.PullSyncDataAsync(knownJobIds);
    }

    private async Task RemoveUnassignedJobsAsync(IEnumerable<Guid> removedJobIds)
    {
        foreach (var id in removedJobIds)
        {
            var cached = await _localDatabaseService.GetJobAsync(id.ToString());
            if (cached is not null)
                await _localDatabaseService.DeleteJobAsync(cached);
        }
    }

    // =========================================================
    // ERROR HELPERS
    // =========================================================

    private void ShowError(
        string message)
    {
        ErrorMessage =
            message;

        HasError =
            true;
    }

    private void ClearError()
    {
        ErrorMessage =
            string.Empty;

        HasError =
            false;
    }

    // =========================================================
    // PROPERTY CHANGED
    // =========================================================

    public event PropertyChangedEventHandler?
        PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}