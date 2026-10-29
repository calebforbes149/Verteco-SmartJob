using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class EquipmentViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly LocalDatabaseService _localDatabaseService;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;

    private string _equipmentName = string.Empty;
    private string _serialNumber = string.Empty;
    private string _assetNumber = string.Empty;
    private string _modelNumber = string.Empty;
    private string _equipmentNotes = string.Empty;

    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;
    private bool _isLoading;
    private bool _hasSavedEquipment;
    private bool _savedDataLoaded;
    private bool _isPendingSync;
    private ApiSiteEquipment? _selectedSiteEquipment;

    public ObservableCollection<ApiSiteEquipment> SiteEquipmentOptions { get; } = new();

    public bool HasSiteEquipmentOptions => SiteEquipmentOptions.Count > 0;

    public ApiSiteEquipment? SelectedSiteEquipment
    {
        get => _selectedSiteEquipment;
        set
        {
            _selectedSiteEquipment = value;
            OnPropertyChanged();
            if (value is null) return;
            EquipmentName = value.Name;
            SerialNumber = value.SerialNumber;
            ReadDescription(value.Description);
        }
    }

    public bool HasSavedEquipment
    {
        get => _hasSavedEquipment;
        private set { _hasSavedEquipment = value; OnPropertyChanged(); }
    }

    // True once equipment has been saved to this device while offline,
    // but has not yet been uploaded to SmartJob by a sync.
    public bool IsEquipmentPendingSync =>
        _hasSavedEquipment && _isPendingSync;

    // True once equipment is confirmed as saved on the job card itself.
    public bool IsEquipmentUploaded =>
        _hasSavedEquipment && !_isPendingSync;

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanContinue)); }
    }

    // =========================================================
    // EQUIPMENT FIELDS
    // =========================================================

    public string EquipmentName
    {
        get => _equipmentName;

        set
        {
            _equipmentName = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public string SerialNumber
    {
        get => _serialNumber;

        set
        {
            _serialNumber = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public string AssetNumber
    {
        get => _assetNumber;

        set
        {
            _assetNumber = value;
            OnPropertyChanged();
        }
    }

    public string ModelNumber
    {
        get => _modelNumber;

        set
        {
            _modelNumber = value;
            OnPropertyChanged();
        }
    }

    public string EquipmentNotes
    {
        get => _equipmentNotes;

        set
        {
            _equipmentNotes = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // UI STATE
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

    public bool CanContinue =>
        !IsSaving && !IsLoading && _savedDataLoaded;

    public ICommand ContinueCommand { get; }
    public ICommand RemoveEquipmentCommand { get; }
    public event EventHandler? EquipmentRemoved;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public EquipmentViewModel(
        Job job,
        LocalDatabaseService localDatabaseService,
        ApiService apiService,
        ConnectivityService connectivityService)
    {
        _job =
            job;

        _localDatabaseService =
            localDatabaseService;

        _apiService =
            apiService;

        _connectivityService =
            connectivityService;

        ContinueCommand =
            new Command(
                async () =>
                    await ContinueAsync());

        RemoveEquipmentCommand = new Command(async () => await RemoveEquipmentAsync());
        _ = LoadEquipmentAsync();
    }

    private async Task LoadEquipmentAsync()
    {
        if (_job.ApiId == Guid.Empty)
        {
            ShowError("SmartJob could not identify the current job.");
            return;
        }

        try
        {
            IsLoading = true;

            // Equipment captured while offline is queued locally until it
            // can be uploaded. Check for one regardless of connectivity,
            // so it's still what gets shown even on a later load.
            EquipmentSyncPayload? pending = await LoadPendingEquipmentPayloadAsync();

            if (!_connectivityService.IsOnline)
            {
                // Offline: there's no point blocking the screen just
                // because we can't ask the server what's already saved -
                // show whatever was captured on this device instead, and
                // let CanContinue unblock so the technician can carry on.
                if (pending is not null)
                {
                    ApplyPendingEquipment(pending);
                }
                else
                {
                    HasSavedEquipment = false;
                    _isPendingSync = false;
                }

                _savedDataLoaded = true;
                OnPropertyChanged(nameof(CanContinue));
                OnPropertyChanged(nameof(IsEquipmentPendingSync));
                OnPropertyChanged(nameof(IsEquipmentUploaded));
                ClearError();
                return;
            }

            var detail = await _apiService.GetJobDetailsAsync(_job.ApiId);
            if (detail is null) return;
            _savedDataLoaded = true;
            OnPropertyChanged(nameof(CanContinue));
            SiteEquipmentOptions.Clear();
            foreach (var option in detail.SiteEquipmentOptions)
                SiteEquipmentOptions.Add(option);
            OnPropertyChanged(nameof(HasSiteEquipmentOptions));

            if (!string.IsNullOrWhiteSpace(detail.FieldEquipmentSerialNumber))
            {
                // The server already has confirmed equipment, so drop any
                // stale locally-queued copy left over from before it synced.
                await RemovePendingEquipmentSyncItemAsync();
                _isPendingSync = false;

                EquipmentName = detail.FieldEquipmentName ?? string.Empty;
                SerialNumber = detail.FieldEquipmentSerialNumber;
                ReadDescription(detail.FieldEquipmentDescription);
                HasSavedEquipment = true;
            }
            else if (pending is not null)
            {
                ApplyPendingEquipment(pending);
            }
            else
            {
                HasSavedEquipment = false;
                _isPendingSync = false;
            }

            OnPropertyChanged(nameof(IsEquipmentPendingSync));
            OnPropertyChanged(nameof(IsEquipmentUploaded));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Equipment load failed: {ex}");
            ShowError("Saved equipment details could not be loaded. Please reconnect and try again.");
        }
        finally { IsLoading = false; }
    }

    private void ApplyPendingEquipment(EquipmentSyncPayload payload)
    {
        _isPendingSync = true;
        EquipmentName = payload.Name;
        SerialNumber = payload.SerialNumber;
        AssetNumber = payload.AssetNumber;
        ModelNumber = payload.ModelNumber;
        EquipmentNotes = payload.Notes;
        HasSavedEquipment = true;
    }

    private void ReadDescription(string? description)
    {
        AssetNumber = string.Empty;
        ModelNumber = string.Empty;
        var notes = new List<string>();
        foreach (var line in (description ?? string.Empty).Split('\n'))
        {
            var text = line.TrimEnd('\r');
            if (text.StartsWith("Asset Number: ", StringComparison.OrdinalIgnoreCase))
                AssetNumber = text[14..];
            else if (text.StartsWith("Model Number: ", StringComparison.OrdinalIgnoreCase))
                ModelNumber = text[14..];
            else if (text.StartsWith("Notes: ", StringComparison.OrdinalIgnoreCase))
                notes.Add(text[7..]);
            else if (!string.IsNullOrWhiteSpace(text))
                notes.Add(text);
        }
        EquipmentNotes = string.Join(Environment.NewLine, notes);
    }

    private async Task RemoveEquipmentAsync()
    {
        if (IsLoading || IsSaving) return;
        if (!_savedDataLoaded)
        {
            ShowError("Saved equipment has not loaded. Reopen this screen while connected to the API.");
            return;
        }
        try
        {
            IsSaving = true;

            if (_isPendingSync)
            {
                // Only a local, not-yet-uploaded copy exists - there's
                // nothing on the server to remove, so just drop it.
                await RemovePendingEquipmentSyncItemAsync();
                _isPendingSync = false;
            }
            else if (HasSavedEquipment)
            {
                if (!_connectivityService.IsOnline)
                {
                    ShowError("Removing saved equipment currently requires an internet connection. Please reconnect before continuing.");
                    return;
                }

                await _apiService.RemoveEquipmentAsync(_job.ApiId);
            }

            HasSavedEquipment = false;
            SelectedSiteEquipment = null;
            EquipmentName = SerialNumber = AssetNumber = ModelNumber = EquipmentNotes = string.Empty;
            OnPropertyChanged(nameof(IsEquipmentPendingSync));
            OnPropertyChanged(nameof(IsEquipmentUploaded));
            EquipmentRemoved?.Invoke(this, EventArgs.Empty);
            ClearError();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Equipment removal failed: {ex}");
            ShowError("The equipment entry could not be removed. Please try again.");
        }
        finally { IsSaving = false; }
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private async Task ContinueAsync()
    {
        ClearError();

        if (IsLoading || IsSaving) return;

        if (string.IsNullOrWhiteSpace(EquipmentName) &&
            string.IsNullOrWhiteSpace(SerialNumber))
        {
            if (HasSavedEquipment)
            {
                await RemoveEquipmentAsync();
                if (HasSavedEquipment) return;
            }
            await Shell.Current.Navigation.PushAsync(new PhotoCapturePage(_job));
            return;
        }

        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
            EquipmentName))
        {
            ShowError(
                "Please enter the equipment name or type.");

            return;
        }

        if (string.IsNullOrWhiteSpace(
            SerialNumber))
        {
            ShowError(
                "Please enter the equipment serial number.");

            return;
        }


        // -----------------------------------------------------
        // REAL JOB ID
        // -----------------------------------------------------

        Guid jobId =
            _job.ApiId;

        if (jobId == Guid.Empty)
        {
            ShowError(
                "SmartJob could not identify the current job.");

            return;
        }

        try
        {
            IsSaving =
                true;

            if (!_connectivityService.IsOnline)
            {
                // Offline: a technician in the field often has no signal,
                // so recording equipment can't require one. Save it to
                // this device and queue it to upload the next time
                // SmartJob syncs.
                await SaveEquipmentForLaterSyncAsync(
                    EquipmentName.Trim(),
                    SerialNumber.Trim(),
                    AssetNumber.Trim(),
                    ModelNumber.Trim(),
                    EquipmentNotes.Trim());

                _isPendingSync = true;
                HasSavedEquipment = true;
                OnPropertyChanged(nameof(IsEquipmentPendingSync));
                OnPropertyChanged(nameof(IsEquipmentUploaded));

                await Shell.Current.Navigation.PushAsync(
                    new PhotoCapturePage(_job));

                return;
            }

            // -------------------------------------------------
            // BUILD DESCRIPTION
            // -------------------------------------------------

            List<string> descriptionParts =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                AssetNumber))
            {
                descriptionParts.Add(
                    $"Asset Number: {AssetNumber.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(
                ModelNumber))
            {
                descriptionParts.Add(
                    $"Model Number: {ModelNumber.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(
                EquipmentNotes))
            {
                descriptionParts.Add(
                    $"Notes: {EquipmentNotes.Trim()}");
            }

            string description =
                string.Join(
                    Environment.NewLine,
                    descriptionParts);

            // -------------------------------------------------
            // CREATE PERSON 4 API REQUEST
            // -------------------------------------------------

            MobileEquipmentRequest request =
                new MobileEquipmentRequest
                {
                    Name =
                        EquipmentName.Trim(),

                    SerialNumber =
                        SerialNumber.Trim(),

                    Description =
                        description
                };

            // -------------------------------------------------
            // SEND TO REAL API
            // -------------------------------------------------

            bool success =
                await _apiService
                    .AddEquipmentAsync(
                        jobId,
                        request);

            if (!success)
            {
                ShowError(
                    "SmartJob could not save the equipment details.");

                return;
            }

            // The upload above succeeded, so drop any stale locally-queued
            // copy from an earlier offline attempt.
            await RemovePendingEquipmentSyncItemAsync();
            _isPendingSync = false;
            HasSavedEquipment = true;
            OnPropertyChanged(nameof(IsEquipmentPendingSync));
            OnPropertyChanged(nameof(IsEquipmentUploaded));

            // -------------------------------------------------
            // CONTINUE TO PHOTO EVIDENCE
            // -------------------------------------------------

            await Shell.Current.Navigation.PushAsync(
                new PhotoCapturePage(_job));
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Equipment API request failed: {ex}");

            ShowError(
                "SmartJob could not connect to the server. Please check the connection and try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Equipment save failed: {ex}");

            ShowError(
                "The equipment details could not be saved. Please try again.");
        }
        finally
        {
            IsSaving =
                false;
        }
    }

    // =========================================================
    // SAVE EQUIPMENT FOR LATER SYNC (OFFLINE)
    // =========================================================

    private async Task SaveEquipmentForLaterSyncAsync(
        string name,
        string serialNumber,
        string assetNumber,
        string modelNumber,
        string notes)
    {
        // Only one pending copy per job - replace whatever was queued
        // before (e.g. from an earlier offline attempt).
        await RemovePendingEquipmentSyncItemAsync();

        EquipmentSyncPayload payload = new EquipmentSyncPayload
        {
            Name = name,
            SerialNumber = serialNumber,
            AssetNumber = assetNumber,
            ModelNumber = modelNumber,
            Notes = notes
        };

        await _localDatabaseService.AddSyncQueueItemAsync(
            new SyncQueueItem
            {
                JobId = _job.ApiId.ToString(),
                OperationType = "UpdateEquipment",
                PayloadJson = JsonSerializer.Serialize(payload)
            });
    }

    // =========================================================
    // OFFLINE EQUIPMENT HELPERS
    // =========================================================

    private async Task<EquipmentSyncPayload?> LoadPendingEquipmentPayloadAsync()
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals("UpdateEquipment", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EquipmentSyncPayload? payload = TryParseEquipmentPayload(item.PayloadJson);
            if (payload is not null) return payload;
        }

        return null;
    }

    private async Task RemovePendingEquipmentSyncItemAsync()
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals("UpdateEquipment", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await _localDatabaseService.DeleteSyncQueueItemAsync(item);
        }
    }

    private static EquipmentSyncPayload? TryParseEquipmentPayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EquipmentSyncPayload>(
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

// =============================================================
// EQUIPMENT SYNC PAYLOAD
//
// What gets stored in a SyncQueueItem.PayloadJson for equipment
// that was recorded offline and is waiting to be uploaded. Read
// back here when reloading the Equipment screen, and read back by
// OfflineSyncViewModel when it actually uploads it.
// =============================================================

public class EquipmentSyncPayload
{
    public string Name { get; set; } = string.Empty;

    public string SerialNumber { get; set; } = string.Empty;

    public string AssetNumber { get; set; } = string.Empty;

    public string ModelNumber { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}