using SmartJobMobile.Models;
using SmartJobMobile.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class OfflineSyncViewModel : INotifyPropertyChanged
{
    private readonly ConnectivityService _connectivityService;
    private readonly LocalDatabaseService _localDatabaseService;

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

    public ICommand SyncCommand { get; }

    public ICommand RefreshCommand { get; }
  
    public OfflineSyncViewModel(
        ConnectivityService connectivityService,
        LocalDatabaseService localDatabaseService)
    {
        _connectivityService =
            connectivityService;

        _localDatabaseService =
            localDatabaseService;

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
                SyncStatus = "Waiting for connection";
                SyncStatusColor = "#FFC107";
            }
        }
        catch (Exception)
        {
            ShowError(
                "The local SmartJob database could not be loaded.");

            SyncStatus = "Local database error";
            SyncStatusColor = "#EF5350";
        }
    }

    private async Task LoadQueueInformationAsync()
    {
        List<SyncQueueItem> pendingItems =
            await _localDatabaseService
                .GetPendingSyncItemsAsync();

        PendingOperations =
            pendingItems.Count;

        PendingEvidence =
            pendingItems.Count(item =>
                item.OperationType == "UploadEvidence");

        PendingJobUpdates =
            pendingItems.Count(item =>
                item.OperationType != "UploadEvidence");

        ConflictCount =
            await _localDatabaseService
                .GetConflictCountAsync();
    }


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

                    SyncStatusColor = "#66BB6A";
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

    private async Task SyncAsync()
    {
        if (IsSyncing)
        {
            return;
        }

        if (!_connectivityService.IsOnline)
        {
            ShowError(
                "You are offline. Connect to the internet before synchronising.");

            SyncStatus =
                "Waiting for connection";

            SyncStatusColor =
                "#FFC107";

            return;
        }

        try
        {
            ClearError();

            IsSyncing = true;

            await LoadQueueInformationAsync();

            if (PendingOperations == 0)
            {
                SyncStatus = "Up to date";
                SyncStatusColor = "#66BB6A";

                LastSync =
                    DateTime.Now.ToString("HH:mm");

                await Shell.Current.DisplayAlert(
                    "Nothing to Sync",
                    "There are no pending offline changes.",
                    "OK");

                return;
            }

            SyncStatus = "Syncing";
            SyncStatusColor = "#FFC107";

            // The queue is now REAL SQLite data.
            //
            // We intentionally do NOT delete the
            // queued items here yet.
            //
            // The future SyncService must first
            // successfully send each operation
            // to Person 4's API before an item
            // can be marked as completed.

            await Task.Delay(750);

            await LoadQueueInformationAsync();

            SyncStatus = "Ready to sync";
            SyncStatusColor = "#66BB6A";

            await Shell.Current.DisplayAlert(
                "Pending API Sync",
                $"{PendingOperations} operation(s) are stored safely on this device. API synchronisation will be connected next.",
                "OK");
        }
        catch (Exception)
        {
            ShowError(
                "SmartJob could not read the local sync queue.");

            SyncStatus = "Sync failed";
            SyncStatusColor = "#EF5350";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            ClearError();

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
                "The local sync status could not be refreshed.");
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}