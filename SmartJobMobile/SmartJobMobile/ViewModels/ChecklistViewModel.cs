using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class ChecklistViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly LocalDatabaseService _localDatabaseService;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;
    private readonly Func<string, bool>? _visibleItem;

    private string _technicianNotes = string.Empty;
    private int _completedCount;
    private bool _isLoading;
    private bool _isSaving;

    // =========================================================
    // JOB INFORMATION
    // =========================================================

    public string JobNumber =>
        _job.JobNumber;

    public string JobTitle =>
        _job.Title;

    public string JobLocation =>
        _job.Location;

    // =========================================================
    // CHECKLIST ITEMS
    // =========================================================

    public ObservableCollection<ChecklistItemViewModel>
        ChecklistItems
    { get; } = new();

    // =========================================================
    // TECHNICIAN NOTES
    // =========================================================

    public string TechnicianNotes
    {
        get => _technicianNotes;

        set
        {
            if (_technicianNotes == value)
            {
                return;
            }

            _technicianNotes = value;

            OnPropertyChanged();
        }
    }

    // =========================================================
    // PROGRESS
    // =========================================================

    public int CompletedCount
    {
        get => _completedCount;

        private set
        {
            if (_completedCount == value)
            {
                return;
            }

            _completedCount = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(Progress));
        }
    }

    public int TotalCount =>
        ChecklistItems.Count;

    public bool HasItems => TotalCount > 0;

    public string ProgressText =>
        $"{CompletedCount} / {TotalCount}";

    public double Progress
    {
        get
        {
            if (TotalCount == 0)
            {
                return 0;
            }

            return (double)CompletedCount /
                   TotalCount;
        }
    }

    // =========================================================
    // LOADING / SAVING
    // =========================================================

    public bool IsLoading
    {
        get => _isLoading;

        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanContinue));
        }
    }

    public bool IsSaving
    {
        get => _isSaving;

        private set
        {
            if (_isSaving == value)
            {
                return;
            }

            _isSaving = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanContinue));
        }
    }

    public bool CanContinue =>
        !IsLoading &&
        !IsSaving &&
        TotalCount > 0;

    // =========================================================
    // COMMAND
    // =========================================================

    public ICommand ContinueCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ChecklistViewModel(
        Job job,
        LocalDatabaseService localDatabaseService,
        ApiService apiService,
        ConnectivityService connectivityService,
        Func<string, bool>? visibleItem = null)
    {
        _job =
            job;

        _localDatabaseService =
            localDatabaseService;

        _apiService =
            apiService;

        _connectivityService =
            connectivityService;

        _visibleItem = visibleItem;

        ContinueCommand =
            new Command(
                async () =>
                    await ContinueAsync());

        _ =
            LoadChecklistAsync();
    }

    // =========================================================
    // LOAD CHECKLIST
    // =========================================================

    private async Task LoadChecklistAsync()
    {
        if (_job.ApiId == Guid.Empty)
        {
            await Shell.Current.DisplayAlert(
                "Invalid Job",
                "This job does not have a valid server ID.",
                "OK");

            return;
        }

        try
        {
            IsLoading = true;

            // =================================================
            // ONLINE
            // =================================================

            if (_connectivityService.IsOnline)
            {
                await LoadChecklistFromServerAsync();
            }

            // =================================================
            // OFFLINE
            // =================================================

            else
            {
                await LoadChecklistFromCacheAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to load checklist: {ex}");

            // If the server call failed, try SQLite before
            // giving up completely.

            try
            {
                await LoadChecklistFromCacheAsync();
            }
            catch
            {
                await Shell.Current.DisplayAlert(
                    "Unable to Load Checklist",
                    "SmartJob could not retrieve the checklist from the server or this device.",
                    "OK");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public Task RefreshAsync() => LoadChecklistAsync();

    // =========================================================
    // LOAD FROM REAL API
    // =========================================================

    private async Task LoadChecklistFromServerAsync()
    {
        ApiJobDetail? detail =
            await _apiService
                .GetJobDetailsAsync(
                    _job.ApiId);

        if (detail == null)
        {
            throw new Exception(
                "The server returned no job details.");
        }

        ChecklistItems.Clear();

        foreach (
            ApiChecklistItem apiItem
            in detail.ChecklistItems
                .OrderBy(item => item.Order)
                .Where(item => _visibleItem?.Invoke(item.Text) ?? true))
        {
            ChecklistItemViewModel item =
                new ChecklistItemViewModel
                {
                    Id =
                        apiItem.Id,

                    Order =
                        apiItem.Order,

                    Text =
                        apiItem.Text
                };

            item.SetCompletedFromServer(
                apiItem.IsDone);

            item.PropertyChanged +=
                ChecklistItem_PropertyChanged;

            ChecklistItems.Add(
                item);
        }

        UpdateProgress();

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(CanContinue));
    }

    // =========================================================
    // LOAD FROM SQLITE
    // =========================================================

    private async Task LoadChecklistFromCacheAsync()
    {
        List<CachedChecklistItem>
            cachedItems =
                await _localDatabaseService
                    .GetChecklistItemsAsync(
                        _job.ApiId.ToString());

        if (cachedItems.Count == 0)
        {
            throw new Exception(
                "No cached checklist exists for this job.");
        }

        ChecklistItems.Clear();

        int order = 0;

        foreach (
            CachedChecklistItem cachedItem
            in cachedItems)
        {
            if (!(_visibleItem?.Invoke(cachedItem.Text) ?? true))
            {
                order++;
                continue;
            }

            ChecklistItemViewModel item =
                new ChecklistItemViewModel
                {
                    Id =
                        Guid.Parse(
                            cachedItem.ChecklistItemId),

                    Order =
                        order,

                    Text =
                        cachedItem.Text
                };

            item.SetCompletedFromServer(
                cachedItem.IsDone);

            item.PropertyChanged +=
                ChecklistItem_PropertyChanged;

            ChecklistItems.Add(
                item);

            order++;
        }

        UpdateProgress();

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(CanContinue));
    }

    // =========================================================
    // CHECKBOX CHANGED
    // =========================================================

    private async void ChecklistItem_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
            nameof(
                ChecklistItemViewModel.IsCompleted))
        {
            return;
        }

        if (sender is not
            ChecklistItemViewModel item)
        {
            return;
        }

        UpdateProgress();

        if (item.IsUpdating)
        {
            return;
        }

        bool requestedValue =
            item.IsCompleted;

        bool previousValue =
            !requestedValue;

        try
        {
            item.IsUpdating =
                true;

            // =================================================
            // ONLINE
            // Send directly to Person 4's API.
            // =================================================

            if (_connectivityService.IsOnline)
            {
                await UpdateChecklistOnlineAsync(
                    item,
                    requestedValue,
                    previousValue);
            }

            // =================================================
            // OFFLINE
            // Save locally and queue the operation.
            // =================================================

            else
            {
                await QueueChecklistChangeAsync(
                    item,
                    requestedValue);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Checklist update failed: {ex}");

            // If we thought we were online but the request
            // failed, attempt to preserve the change locally.

            try
            {
                await QueueChecklistChangeAsync(
                    item,
                    requestedValue);

                await Shell.Current.DisplayAlert(
                    "Saved Offline",
                    "The server could not be reached. Your checklist change has been saved on this device and will be synchronised later.",
                    "OK");
            }
            catch (Exception queueException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Offline queue failed: {queueException}");

                item.SetCompletedFromServer(
                    previousValue);

                UpdateProgress();

                await Shell.Current.DisplayAlert(
                    "Unable to Save",
                    "The checklist change could not be saved to the server or this device.",
                    "OK");
            }
        }
        finally
        {
            item.IsUpdating =
                false;
        }
    }

    // =========================================================
    // ONLINE CHECKLIST UPDATE
    // =========================================================

    private async Task UpdateChecklistOnlineAsync(
        ChecklistItemViewModel item,
        bool requestedValue,
        bool previousValue)
    {
        ApiJobDetail? updatedJob =
            await _apiService
                .ToggleChecklistItemAsync(
                    _job.ApiId,
                    item.Id);

        if (updatedJob == null)
        {
            item.SetCompletedFromServer(
                previousValue);

            UpdateProgress();

            throw new Exception(
                "The server returned no updated job.");
        }

        ApiChecklistItem? serverItem =
            updatedJob.ChecklistItems
                .FirstOrDefault(
                    checklistItem =>
                        checklistItem.Id ==
                        item.Id);

        if (serverItem == null)
        {
            item.SetCompletedFromServer(
                previousValue);

            UpdateProgress();

            throw new Exception(
                "The updated checklist item was not returned by the server.");
        }

        // Server remains source of truth.

        item.SetCompletedFromServer(
            serverItem.IsDone);

        UpdateProgress();

        // Refresh RowVersions after successful online change.
        // The Sync Pull endpoint provides the RowVersions that
        // the normal job-detail endpoint does not expose.

        try
        {
            await RefreshSyncCacheAsync();
        }
        catch (Exception ex)
        {
            // The checklist change itself already succeeded.
            // A cache refresh failure should therefore not
            // undo the user's successful server change.

            System.Diagnostics.Debug.WriteLine(
                $"RowVersion cache refresh failed: {ex}");
        }
    }

    // =========================================================
    // QUEUE OFFLINE CHECKLIST CHANGE
    // =========================================================

    private async Task QueueChecklistChangeAsync(
        ChecklistItemViewModel item,
        bool requestedValue)
    {
        string jobId =
            _job.ApiId.ToString();

        string checklistItemId =
            item.Id.ToString();

        // Get the RowVersions saved by Sync Pull.

        CachedJob? cachedJob =
            await _localDatabaseService
                .GetJobAsync(
                    jobId);

        if (cachedJob == null)
        {
            throw new Exception(
                "This job has not been cached for offline use.");
        }

        CachedChecklistItem? cachedChecklistItem =
            await _localDatabaseService
                .GetChecklistItemAsync(
                    checklistItemId);

        if (cachedChecklistItem == null)
        {
            throw new Exception(
                "This checklist item has not been cached for offline use.");
        }

        if (string.IsNullOrWhiteSpace(
            cachedJob.RowVersion))
        {
            throw new Exception(
                "The cached job does not contain a RowVersion.");
        }

        if (string.IsNullOrWhiteSpace(
            cachedChecklistItem.RowVersion))
        {
            throw new Exception(
                "The cached checklist item does not contain a RowVersion.");
        }

        // =====================================================
        // CREATE PAYLOAD
        // =====================================================

        ChecklistSyncPayload payload =
            new ChecklistSyncPayload
            {
                ChecklistItemId =
                    item.Id,

                ChecklistItemRowVersion =
                    cachedChecklistItem.RowVersion,

                IsDone =
                    requestedValue
            };

        string payloadJson =
            JsonSerializer.Serialize(
                payload);

        // =====================================================
        // CREATE QUEUE ITEM
        // =====================================================

        SyncQueueItem queueItem =
            new SyncQueueItem
            {
                IdempotencyKey =
                    Guid.NewGuid()
                        .ToString(),

                JobId =
                    jobId,

                OperationType =
                    "setchecklistitem",

                PayloadJson =
                    payloadJson,

                BaseRowVersion =
                    cachedJob.RowVersion,

                Status =
                    "Pending"
            };

        await _localDatabaseService
            .AddSyncQueueItemAsync(
                queueItem);

        // =====================================================
        // UPDATE SQLITE CHECKLIST VALUE
        // =====================================================

        cachedChecklistItem.IsDone =
            requestedValue;

        await _localDatabaseService
            .SaveChecklistItemAsync(
                cachedChecklistItem);

        // Keep the value selected in the UI.

        item.SetCompletedFromServer(
            requestedValue);

        UpdateProgress();

        System.Diagnostics.Debug.WriteLine(
            $"Queued offline checklist operation: {queueItem.IdempotencyKey}");
    }

    // =========================================================
    // REFRESH SQLITE ROWVERSIONS
    // =========================================================

    private async Task RefreshSyncCacheAsync()
    {
        MobileSyncPullResponse? response =
            await _apiService
                .PullSyncDataAsync();

        if (response == null)
        {
            return;
        }

        MobileSyncJob? serverJob =
            response.Jobs
                .FirstOrDefault(
                    job =>
                        job.Id ==
                        _job.ApiId);

        if (serverJob == null)
        {
            return;
        }

        CachedJob cachedJob =
            new CachedJob
            {
                JobId =
                    serverJob.Id
                        .ToString(),

                JobNumber =
                    serverJob.Number,

                Title =
                    _job.Title,

                ClientName =
                    _job.CustomerName,

                Location =
                    _job.Location,

                Status =
                    serverJob.Status,

                Priority =
                    _job.Priority,

                ScheduledDate =
                    serverJob.ScheduledDate
                        .ToDateTime(
                            TimeOnly.MinValue),

                Description =
                    serverJob.Notes
                    ?? string.Empty,

                RowVersion =
                    serverJob.RowVersion,

                HasLocalChanges =
                    false
            };

        await _localDatabaseService
            .SaveJobAsync(
                cachedJob);

        foreach (
            MobileSyncChecklistItem serverItem
            in serverJob.ChecklistItems)
        {
            CachedChecklistItem cachedItem =
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
                };

            await _localDatabaseService
                .SaveChecklistItemAsync(
                    cachedItem);
        }
    }

    // =========================================================
    // UPDATE PROGRESS
    // =========================================================

    private void UpdateProgress()
    {
        CompletedCount =
            ChecklistItems.Count(
                item => item.IsCompleted);

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(CanContinue));
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private async Task ContinueAsync()
    {
        if (TotalCount == 0)
        {
            await Shell.Current.DisplayAlert(
                "Checklist Unavailable",
                "There are no checklist items available for this job.",
                "OK");

            return;
        }

        if (IsSaving)
        {
            return;
        }

        try
        {
            IsSaving =
                true;

            // Checklist changes are now saved individually:
            //
            // ONLINE:
            // sent directly to the API.
            //
            // OFFLINE:
            // saved to SQLite and queued as
            // "setchecklistitem".
            //
            // Therefore we DO NOT create the old
            // "UpdateChecklist" queue operation here.

            await Shell.Current.Navigation.PushAsync(
                new EquipmentPage(_job));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to continue from checklist: {ex}");

            await Shell.Current.DisplayAlert(
                "Navigation Error",
                "SmartJob could not continue to the equipment screen.",
                "OK");
        }
        finally
        {
            IsSaving =
                false;
        }
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


// =============================================================
// INDIVIDUAL CHECKLIST ITEM
// =============================================================

public class ChecklistItemViewModel :
    INotifyPropertyChanged
{
    private bool _isCompleted;
    private bool _isUpdating;

    public Guid Id { get; set; }

    public int Order { get; set; }

    public string Text { get; set; } =
        string.Empty;

    // API order starts at 0.
    // User-facing numbering starts at 1.

    public string DisplayText =>
        $"{Order + 1}. {Text}";

    public bool IsCompleted
    {
        get => _isCompleted;

        set
        {
            if (_isCompleted == value)
            {
                return;
            }

            _isCompleted =
                value;

            OnPropertyChanged();
        }
    }

    public bool IsUpdating
    {
        get => _isUpdating;

        set
        {
            if (_isUpdating == value)
            {
                return;
            }

            _isUpdating =
                value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEdit));
        }
    }

    public bool CanEdit => !IsUpdating;

    public void SetCompletedFromServer(
        bool value)
    {
        _isCompleted =
            value;

        OnPropertyChanged(
            nameof(IsCompleted));
    }

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


// =============================================================
// OFFLINE CHECKLIST SYNC PAYLOAD
// =============================================================

public class ChecklistSyncPayload
{
    public Guid ChecklistItemId { get; set; }

    public string ChecklistItemRowVersion
    {
        get;
        set;
    } = string.Empty;

    public bool IsDone { get; set; }
}