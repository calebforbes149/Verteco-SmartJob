using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class CommentsFlagViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly LocalDatabaseService _localDatabaseService;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;

    private string _comments = string.Empty;
    private bool _isFlagged;
    private string? _selectedFlagReason;
    private string _flagDetails = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;
    private bool _isLoading;
    private bool _commentsTouched;
    private bool _alreadyFlagged;
    private bool _flagPendingSync;
    private bool _savedDataLoaded;

    public bool CanChangeFlag => !_alreadyFlagged;

    // True once the flag has been saved to this device while offline,
    // but has not yet been uploaded to SmartJob by a sync.
    public bool IsFlagPendingSync => _alreadyFlagged && _flagPendingSync;

    // True once the flag is confirmed as saved on the job card itself.
    public bool IsFlagUploaded => _alreadyFlagged && !_flagPendingSync;

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanContinue)); }
    }

    // =========================================================
    // REAL JOB INFORMATION
    // =========================================================

    public string JobNumber => _job.JobNumber;

    public string JobTitle => _job.Title;

    public string JobLocation => _job.Location;

    // =========================================================
    // COMMENTS
    // =========================================================

    public string Comments
    {
        get => _comments;
        set
        {
            if (_comments == value) return;
            _comments = value;
            _commentsTouched = true;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // FLAG JOB
    // =========================================================

    public bool IsFlagged
    {
        get => _isFlagged;
        set
        {
            _isFlagged = value;
            OnPropertyChanged();

            if (!value)
            {
                SelectedFlagReason = null;
                FlagDetails = string.Empty;
            }

            ClearError();
        }
    }

    public ObservableCollection<string> FlagReasons { get; } =
        new()
        {
            "Equipment damaged",
            "Safety concern",
            "Additional repair required",
            "Replacement part required",
            "Unable to complete job",
            "Client issue",
            "Other"
        };

    public string? SelectedFlagReason
    {
        get => _selectedFlagReason;
        set
        {
            _selectedFlagReason = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public string FlagDetails
    {
        get => _flagDetails;
        set
        {
            _flagDetails = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    // =========================================================
    // ERROR STATE
    // =========================================================

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set
        {
            _hasError = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // SAVING STATE
    // =========================================================

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanContinue));
        }
    }

    public bool CanContinue => !IsSaving && !IsLoading && _savedDataLoaded;

    public ICommand ContinueCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CommentsFlagViewModel(
        Job job,
        LocalDatabaseService localDatabaseService,
        ApiService apiService,
        ConnectivityService connectivityService)
    {
        _job = job;
        _localDatabaseService = localDatabaseService;
        _apiService = apiService;
        _connectivityService = connectivityService;

        ContinueCommand =
            new Command(
                async () => await ContinueAsync());

        _ = LoadSavedWorkAsync();
    }

    private async Task LoadSavedWorkAsync()
    {
        if (_job.ApiId == Guid.Empty)
        {
            ShowError("SmartJob could not identify the current job.");
            return;
        }

        try
        {
            IsLoading = true;

            // Comments or a flag saved while offline are queued locally
            // until they can be uploaded. Check for either regardless of
            // connectivity, so they're still what gets shown on a later
            // load even before a sync has run.
            CommentsSyncPayload? pendingComments = await LoadPendingCommentsPayloadAsync();
            FlagSyncPayload? pendingFlag = await LoadPendingFlagPayloadAsync();

            if (!_connectivityService.IsOnline)
            {
                // Offline: there's no point blocking the screen just
                // because we can't ask the server what's already saved -
                // show whatever was captured on this device instead, and
                // let CanContinue unblock so the technician can carry on.
                if (!_commentsTouched && pendingComments is not null)
                {
                    _comments = pendingComments.Comments;
                    OnPropertyChanged(nameof(Comments));
                }

                if (pendingFlag is not null)
                {
                    ApplyPendingFlag(pendingFlag);
                }

                _savedDataLoaded = true;
                OnPropertyChanged(nameof(CanContinue));
                OnPropertyChanged(nameof(IsFlagPendingSync));
                OnPropertyChanged(nameof(IsFlagUploaded));
                ClearError();
                return;
            }

            var detail = await _apiService.GetJobDetailsAsync(_job.ApiId);
            if (detail is null) return;
            _savedDataLoaded = true;
            OnPropertyChanged(nameof(CanContinue));
            if (!_commentsTouched)
            {
                // Server comments are authoritative once confirmed, but a
                // locally-queued edit hasn't reached the server yet, so
                // prefer it if one exists.
                if (pendingComments is not null)
                {
                    _comments = pendingComments.Comments;
                }
                else
                {
                    _comments = detail.TechnicianComments ?? string.Empty;
                }
                OnPropertyChanged(nameof(Comments));
            }

            if (detail.IsFlagged)
            {
                // The server already has a confirmed flag, so drop any
                // stale locally-queued copy left over from before it synced.
                await RemovePendingFlagSyncItemAsync();
                _flagPendingSync = false;

                _alreadyFlagged = true;
                OnPropertyChanged(nameof(CanChangeFlag));
                IsFlagged = true;
                var reason = detail.FlagReason ?? string.Empty;
                SelectedFlagReason = FlagReasons.FirstOrDefault(option =>
                    reason.StartsWith(option + ":", StringComparison.OrdinalIgnoreCase)) ?? "Other";
                FlagDetails = reason.Contains(':') ? reason[(reason.IndexOf(':') + 1)..].Trim() : reason;
            }
            else if (pendingFlag is not null)
            {
                ApplyPendingFlag(pendingFlag);
            }
            else
            {
                _alreadyFlagged = false;
                _flagPendingSync = false;
                OnPropertyChanged(nameof(CanChangeFlag));
            }

            OnPropertyChanged(nameof(IsFlagPendingSync));
            OnPropertyChanged(nameof(IsFlagUploaded));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Saved comments load failed: {ex}");
            ShowError("Saved comments could not be loaded. Please reconnect and try again.");
        }
        finally { IsLoading = false; }
    }

    private void ApplyPendingFlag(FlagSyncPayload payload)
    {
        _flagPendingSync = true;
        _alreadyFlagged = true;
        OnPropertyChanged(nameof(CanChangeFlag));
        IsFlagged = true;

        string reason = payload.Reason;
        SelectedFlagReason = FlagReasons.FirstOrDefault(option =>
            reason.StartsWith(option + ":", StringComparison.OrdinalIgnoreCase)) ?? "Other";
        FlagDetails = reason.Contains(':') ? reason[(reason.IndexOf(':') + 1)..].Trim() : reason;
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private async Task ContinueAsync()
    {
        ClearError();

        if (IsLoading || IsSaving) return;
        if (!_savedDataLoaded)
        {
            ShowError("Saved comments have not loaded. Reopen this screen while connected to the API.");
            return;
        }

        if (_job.ApiId == Guid.Empty)
        {
            ShowError(
                "SmartJob could not identify the current job.");

            return;
        }

        // A reason and description are required
        // when the technician flags the job.
        if (IsFlagged && !_alreadyFlagged)
        {
            if (string.IsNullOrWhiteSpace(
                SelectedFlagReason))
            {
                ShowError(
                    "Please select a reason for flagging this job.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                FlagDetails))
            {
                ShowError(
                    "Please describe the issue that requires attention.");

                return;
            }
        }

        try
        {
            IsSaving = true;

            // Only touch the backend when the technician actually
            // changed something.
            if (_commentsTouched)
            {
                if (_connectivityService.IsOnline)
                {
                    await _apiService.SaveTechnicianCommentsAsync(_job.ApiId, Comments);
                }
                else
                {
                    // Offline: a technician in the field often has no
                    // signal, so saving comments can't require one. Queue
                    // them to upload the next time SmartJob syncs.
                    await SaveCommentsForLaterSyncAsync(Comments);
                }

                _commentsTouched = false;
            }

            if (IsFlagged && !_alreadyFlagged)
            {
                string reason =
                    $"{SelectedFlagReason}: {FlagDetails.Trim()}";

                if (_connectivityService.IsOnline)
                {
                    await _apiService.FlagJobAsync(
                        _job.ApiId,
                        reason);
                    _flagPendingSync = false;
                }
                else
                {
                    // Offline: queue the flag to upload the next time
                    // SmartJob syncs, same reasoning as comments above.
                    await SaveFlagForLaterSyncAsync(reason);
                    _flagPendingSync = true;
                }

                _alreadyFlagged = true;
                OnPropertyChanged(nameof(CanChangeFlag));
                OnPropertyChanged(nameof(IsFlagPendingSync));
                OnPropertyChanged(nameof(IsFlagUploaded));
            }

            await Shell.Current.Navigation.PushAsync(
                new SignaturePage(_job));
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Flag job HTTP error: {ex}");

            ShowError(
                "SmartJob could not save the job flag. Please check the connection and try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Comments and flag error: {ex}");

            ShowError(
                "The comments and flag information could not be processed. Please try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    // =========================================================
    // SAVE FOR LATER SYNC (OFFLINE)
    // =========================================================

    private async Task SaveCommentsForLaterSyncAsync(string comments)
    {
        // Only one pending copy per job - replace whatever was queued
        // before (e.g. from an earlier offline attempt).
        await RemovePendingQueueItemAsync("SaveComments");

        CommentsSyncPayload payload = new CommentsSyncPayload { Comments = comments };

        await _localDatabaseService.AddSyncQueueItemAsync(
            new SyncQueueItem
            {
                JobId = _job.ApiId.ToString(),
                OperationType = "SaveComments",
                PayloadJson = JsonSerializer.Serialize(payload)
            });
    }

    private async Task SaveFlagForLaterSyncAsync(string reason)
    {
        await RemovePendingQueueItemAsync("FlagJob");

        FlagSyncPayload payload = new FlagSyncPayload { Reason = reason };

        await _localDatabaseService.AddSyncQueueItemAsync(
            new SyncQueueItem
            {
                JobId = _job.ApiId.ToString(),
                OperationType = "FlagJob",
                PayloadJson = JsonSerializer.Serialize(payload)
            });
    }

    // =========================================================
    // OFFLINE COMMENTS/FLAG HELPERS
    // =========================================================

    private async Task<CommentsSyncPayload?> LoadPendingCommentsPayloadAsync()
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals("SaveComments", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            CommentsSyncPayload? payload = TryParseCommentsPayload(item.PayloadJson);
            if (payload is not null) return payload;
        }

        return null;
    }

    private async Task<FlagSyncPayload?> LoadPendingFlagPayloadAsync()
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals("FlagJob", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            FlagSyncPayload? payload = TryParseFlagPayload(item.PayloadJson);
            if (payload is not null) return payload;
        }

        return null;
    }

    private async Task RemovePendingQueueItemAsync(string operationType)
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await _localDatabaseService.DeleteSyncQueueItemAsync(item);
        }
    }

    private async Task RemovePendingFlagSyncItemAsync() =>
        await RemovePendingQueueItemAsync("FlagJob");

    private static CommentsSyncPayload? TryParseCommentsPayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CommentsSyncPayload>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static FlagSyncPayload? TryParseFlagPayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<FlagSyncPayload>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    // =========================================================
    // ERROR HELPERS
    // =========================================================

    private void ShowError(
        string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    // =========================================================
    // PROPERTY CHANGED
    // =========================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}

// =============================================================
// COMMENTS / FLAG SYNC PAYLOADS
//
// What gets stored in a SyncQueueItem.PayloadJson for comments or a
// flag saved offline and waiting to be uploaded. Read back here when
// reloading the Comments & Flag screen, and read back by
// OfflineSyncViewModel when it actually uploads them.
// =============================================================

public class CommentsSyncPayload
{
    public string Comments { get; set; } = string.Empty;
}

public class FlagSyncPayload
{
    public string Reason { get; set; } = string.Empty;
}