using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace SmartJobMobile.ViewModels;

public class SignatureViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly LocalDatabaseService _localDatabaseService;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;

    private string _technicianName = string.Empty;
    private string _clientName = string.Empty;

    private bool _technicianConfirmed;
    private bool _clientConfirmed;

    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;
    private bool _isLoadingExisting;
    private bool _savedDataLoaded;
    private Guid? _technicianSignatureId;
    private Guid? _clientSignatureId;
    private string _savedTechnicianName = string.Empty;
    private string _savedClientName = string.Empty;
    private ImageSource? _technicianSignaturePreview;
    private ImageSource? _clientSignaturePreview;
    private bool _isReplacingTechnicianSignature;
    private bool _isReplacingClientSignature;

    // True when a signature has been captured and queued on this device
    // but not uploaded yet (captured while offline).
    private bool _isTechnicianPendingSync;
    private bool _isClientPendingSync;

    // Fired right after signatures are confirmed on the server, so the page
    // can clear the drawing pads before the user could tap Continue again.
    public event Action? SignaturesUploaded;

    // A signature "exists" whether it's already confirmed on the server or
    // only captured locally and waiting to sync - either way there's
    // something to show and nothing more to draw.
    public bool HasTechnicianSignature => _technicianSignatureId.HasValue || _isTechnicianPendingSync;
    public bool HasClientSignature => _clientSignatureId.HasValue || _isClientPendingSync;

    public bool IsTechnicianSignaturePendingSync => _isTechnicianPendingSync;
    public bool IsClientSignaturePendingSync => _isClientPendingSync;

    // "Uploaded" (confirmed on the server) is the opposite of "pending
    // sync" whenever there's a signature at all - kept as its own
    // property instead of inverting IsTechnicianSignaturePendingSync in
    // XAML, since this project has no bool-inverse converter registered.
    public bool IsTechnicianSignatureUploaded => _technicianSignatureId.HasValue;
    public bool IsClientSignatureUploaded => _clientSignatureId.HasValue;

    // The blank drawing pad only needs to be shown when there isn't already
    // a saved signature, or when the user explicitly asked to replace it.
    // Previously the pad had no visibility binding at all, so it stayed on
    // screen underneath the "saved signature" preview the moment a reload
    // picked up a signature that had already been uploaded - this is what
    // looked like a duplicate signature box.
    public bool ShowTechnicianPad => !HasTechnicianSignature || _isReplacingTechnicianSignature;
    public bool ShowClientPad => !HasClientSignature || _isReplacingClientSignature;

    public bool CanContinue => !IsSaving && !IsLoadingExisting && _savedDataLoaded;
    public bool IsLoadingExisting
    {
        get => _isLoadingExisting;
        private set { _isLoadingExisting = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanContinue)); }
    }
    public ImageSource? TechnicianSignaturePreview
    {
        get => _technicianSignaturePreview;
        private set { _technicianSignaturePreview = value; OnPropertyChanged(); }
    }
    public ImageSource? ClientSignaturePreview
    {
        get => _clientSignaturePreview;
        private set { _clientSignaturePreview = value; OnPropertyChanged(); }
    }

    // =========================================================
    // JOB INFORMATION
    // =========================================================

    public string JobNumber => _job.JobNumber;

    public string JobTitle => _job.Title;

    public string JobLocation => _job.Location;

    // =========================================================
    // TECHNICIAN
    // =========================================================

    public string TechnicianName
    {
        get => _technicianName;
        set
        {
            _technicianName = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public bool TechnicianConfirmed
    {
        get => _technicianConfirmed;
        set
        {
            _technicianConfirmed = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    // =========================================================
    // CLIENT
    // =========================================================

    public string ClientName
    {
        get => _clientName;
        set
        {
            _clientName = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public bool ClientConfirmed
    {
        get => _clientConfirmed;
        set
        {
            _clientConfirmed = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    // =========================================================
    // PAGE STATE
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

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public SignatureViewModel(
        Job job,
        LocalDatabaseService localDatabaseService,
        ApiService apiService,
        ConnectivityService connectivityService)
    {
        _job = job;
        _localDatabaseService = localDatabaseService;
        _apiService = apiService;
        _connectivityService = connectivityService;
        _ = LoadExistingSignaturesAsync();
    }

    private async Task LoadExistingSignaturesAsync()
    {
        if (_job.ApiId == Guid.Empty)
        {
            ShowError("SmartJob could not identify the current job.");
            return;
        }

        try
        {
            IsLoadingExisting = true;

            // A signature captured while offline is queued locally until it
            // can be uploaded. Check for one regardless of connectivity, so
            // it's still what gets shown even on a later load.
            (SignatureSyncPayload? pendingTechnician, SignatureSyncPayload? pendingClient) =
                await LoadPendingSignaturePayloadsAsync();

            if (!_connectivityService.IsOnline)
            {
                // Offline: there's no point blocking the screen just
                // because we can't ask the server what's already saved -
                // show whatever was captured on this device instead, and
                // let CanContinue unblock so signing can still happen.
                //
                // Only touch a side that isn't already confirmed from an
                // earlier online load - we have no way to re-fetch a
                // confirmed signature's preview right now, so leave it
                // exactly as it was rather than blanking it out.
                if (!_technicianSignatureId.HasValue)
                {
                    _isTechnicianPendingSync = false;
                    TechnicianSignaturePreview = null;
                    if (pendingTechnician is not null) ApplyPendingTechnicianSignature(pendingTechnician);
                }

                if (!_clientSignatureId.HasValue)
                {
                    _isClientPendingSync = false;
                    ClientSignaturePreview = null;
                    if (pendingClient is not null) ApplyPendingClientSignature(pendingClient);
                }

                _isReplacingTechnicianSignature = false;
                _isReplacingClientSignature = false;

                _savedDataLoaded = true;
                OnPropertyChanged(nameof(CanContinue));
                OnPropertyChanged(nameof(HasTechnicianSignature));
                OnPropertyChanged(nameof(HasClientSignature));
                OnPropertyChanged(nameof(ShowTechnicianPad));
                OnPropertyChanged(nameof(ShowClientPad));
                OnPropertyChanged(nameof(IsTechnicianSignaturePendingSync));
                OnPropertyChanged(nameof(IsClientSignaturePendingSync));
                OnPropertyChanged(nameof(IsTechnicianSignatureUploaded));
                OnPropertyChanged(nameof(IsClientSignatureUploaded));

                ClearError();
                return;
            }

            var detail = await _apiService.GetJobDetailsAsync(_job.ApiId);
            if (detail is null) return;
            _savedDataLoaded = true;
            OnPropertyChanged(nameof(CanContinue));
            var technician = detail.Signatures.LastOrDefault(s => s.Type == "Technician");
            var client = detail.Signatures.LastOrDefault(s => s.Type == "Client");
            _technicianSignatureId = technician?.Id;
            _clientSignatureId = client?.Id;
            TechnicianSignaturePreview = null;
            ClientSignaturePreview = null;
            // A fresh load always reflects the server's current state, so
            // collapse back to "show the saved signature" rather than
            // leaving a stale pad open from an earlier replace request.
            _isReplacingTechnicianSignature = false;
            _isReplacingClientSignature = false;

            if (technician is not null)
            {
                // The server already has a confirmed signature, so drop any
                // stale locally-queued copy left over from before it synced.
                await ClearPendingSignatureAsync("Technician");
                _isTechnicianPendingSync = false;

                _savedTechnicianName = technician.SignerName;
                TechnicianName = technician.SignerName;
                try
                {
                    var bytes = await _apiService.GetSignatureFileAsync(_job.ApiId, technician.Id);
                    TechnicianSignaturePreview = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Technician signature preview unavailable: {ex}"); }
            }
            else if (pendingTechnician is not null)
            {
                ApplyPendingTechnicianSignature(pendingTechnician);
            }
            else
            {
                _savedTechnicianName = string.Empty;
                _isTechnicianPendingSync = false;
            }

            if (client is not null)
            {
                await ClearPendingSignatureAsync("Client");
                _isClientPendingSync = false;

                _savedClientName = client.SignerName;
                ClientName = client.SignerName;
                try
                {
                    var bytes = await _apiService.GetSignatureFileAsync(_job.ApiId, client.Id);
                    ClientSignaturePreview = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Client signature preview unavailable: {ex}"); }
            }
            else if (pendingClient is not null)
            {
                ApplyPendingClientSignature(pendingClient);
            }
            else
            {
                _savedClientName = string.Empty;
                _isClientPendingSync = false;
            }

            OnPropertyChanged(nameof(HasTechnicianSignature));
            OnPropertyChanged(nameof(HasClientSignature));
            OnPropertyChanged(nameof(ShowTechnicianPad));
            OnPropertyChanged(nameof(ShowClientPad));
            OnPropertyChanged(nameof(IsTechnicianSignaturePendingSync));
            OnPropertyChanged(nameof(IsClientSignaturePendingSync));
            OnPropertyChanged(nameof(IsTechnicianSignatureUploaded));
            OnPropertyChanged(nameof(IsClientSignatureUploaded));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Saved signatures load failed: {ex}");
            ShowError("Saved signatures could not be loaded. Please reconnect and try again.");
        }
        finally { IsLoadingExisting = false; }
    }

    private void ApplyPendingTechnicianSignature(SignatureSyncPayload payload)
    {
        _isTechnicianPendingSync = true;
        _savedTechnicianName = payload.SignerName;
        TechnicianName = payload.SignerName;

        if (File.Exists(payload.LocalFilePath))
        {
            byte[] bytes = File.ReadAllBytes(payload.LocalFilePath);
            TechnicianSignaturePreview = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
    }

    private void ApplyPendingClientSignature(SignatureSyncPayload payload)
    {
        _isClientPendingSync = true;
        _savedClientName = payload.SignerName;
        ClientName = payload.SignerName;

        if (File.Exists(payload.LocalFilePath))
        {
            byte[] bytes = File.ReadAllBytes(payload.LocalFilePath);
            ClientSignaturePreview = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
    }

    public async Task RemoveExistingSignatureAsync(string type)
    {
        var id = type == "Technician" ? _technicianSignatureId : _clientSignatureId;
        bool isPendingOnly = type == "Technician" ? _isTechnicianPendingSync : _isClientPendingSync;

        if (!id.HasValue && !isPendingOnly) return;
        if (IsSaving) return;

        try
        {
            IsSaving = true;

            if (id.HasValue)
            {
                // Confirmed on the server - remove it there.
                await _apiService.RemoveSignatureAsync(_job.ApiId, id.Value);
            }
            else
            {
                // Only captured on this device and never uploaded - just
                // drop the local file and its queued upload.
                await ClearPendingSignatureAsync(type);
            }

            if (type == "Technician")
            {
                _technicianSignatureId = null;
                _isTechnicianPendingSync = false;
                TechnicianSignaturePreview = null;
                TechnicianConfirmed = false;
            }
            else
            {
                _clientSignatureId = null;
                _isClientPendingSync = false;
                ClientSignaturePreview = null;
                ClientConfirmed = false;
            }
            await LoadExistingSignaturesAsync();
            ClearError();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Signature removal failed: {ex}");
            ShowError("The saved signature could not be removed. Please try again.");
        }
        finally { IsSaving = false; }
    }

    // =========================================================
    // REPLACE A SAVED SIGNATURE
    // =========================================================

    public void BeginReplaceTechnicianSignature()
    {
        _isReplacingTechnicianSignature = true;
        OnPropertyChanged(nameof(ShowTechnicianPad));
    }

    public void BeginReplaceClientSignature()
    {
        _isReplacingClientSignature = true;
        OnPropertyChanged(nameof(ShowClientPad));
    }

    // =========================================================
    // SUBMIT DRAWN SIGNATURES
    // =========================================================

    public async Task SubmitDrawnSignaturesAsync(
        string? technicianSignaturePath,
        string? clientSignaturePath)
    {
        ClearError();

        if (IsLoadingExisting || IsSaving) return;

        // Make sure this is a real backend job.
        if (_job.ApiId == Guid.Empty)
        {
            ShowError(
                "SmartJob could not identify the current job.");

            return;
        }

        if (technicianSignaturePath is null && HasTechnicianSignature &&
            !string.Equals(TechnicianName, _savedTechnicianName, StringComparison.Ordinal))
        {
            ShowError("Draw a new technician signature to change the signer name.");
            return;
        }

        if (clientSignaturePath is null && HasClientSignature &&
            !string.Equals(ClientName, _savedClientName, StringComparison.Ordinal))
        {
            ShowError("Draw a new client signature to change the signer name.");
            return;
        }

        // Technician name.
        if (technicianSignaturePath is not null && string.IsNullOrWhiteSpace(TechnicianName))
        {
            ShowError(
                "Please enter the technician name.");

            return;
        }

        // Technician confirmation.
        if (technicianSignaturePath is not null && !TechnicianConfirmed)
        {
            ShowError(
                "The technician must confirm the job information.");

            return;
        }

        // Technician signature file.
        if ((technicianSignaturePath is null && !HasTechnicianSignature) ||
            (technicianSignaturePath is not null && !File.Exists(technicianSignaturePath)))
        {
            ShowError(
                "Please provide the technician signature.");

            return;
        }

        // Client name.
        if (clientSignaturePath is not null && string.IsNullOrWhiteSpace(ClientName))
        {
            ShowError(
                "Please enter the client name.");

            return;
        }

        // Client confirmation.
        if (clientSignaturePath is not null && !ClientConfirmed)
        {
            ShowError(
                "The client must confirm the completed work.");

            return;
        }

        // Client signature file.
        if ((clientSignaturePath is null && !HasClientSignature) ||
            (clientSignaturePath is not null && !File.Exists(clientSignaturePath)))
        {
            ShowError(
                "Please provide the client signature.");

            return;
        }

        if (IsSaving)
        {
            return;
        }

        try
        {
            IsSaving = true;

            try
            {
                if (_connectivityService.IsOnline)
                {
                    // Online: upload (and confirm) right away, same as before.
                    if (technicianSignaturePath is not null)
                        await UploadSignatureAsync(
                            technicianSignaturePath,
                            TechnicianName.Trim(),
                            "Technician");

                    if (clientSignaturePath is not null)
                        await UploadSignatureAsync(
                            clientSignaturePath,
                            ClientName.Trim(),
                            "Client");
                }
                else
                {
                    // Offline: a technician in the field often has no
                    // signal, so capturing a signature can't require one.
                    // Save it to this device and queue it to upload the
                    // next time SmartJob syncs.
                    if (technicianSignaturePath is not null)
                        await SaveSignatureForLaterSyncAsync(
                            technicianSignaturePath,
                            TechnicianName.Trim(),
                            "Technician");

                    if (clientSignaturePath is not null)
                        await SaveSignatureForLaterSyncAsync(
                            clientSignaturePath,
                            ClientName.Trim(),
                            "Client");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Signature Network Error: {ex}");

                ShowError(
                    "SmartJob could not upload the signatures. Please check the connection and try again.");

                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Signature Save Error: {ex}");

                ShowError(
                    "The signatures could not be saved. Please try again.");

                return;
            }

            // The uploads above succeeded, so refresh local state from the
            // server now - this hides the drawing pad for whichever
            // signature just got saved, and means the page shows the right
            // thing even if opening the review page below fails. It also
            // tells the page to clear the pads so a retry doesn't re-upload
            // the same signature a second time.
            await LoadExistingSignaturesAsync();
            SignaturesUploaded?.Invoke();

            try
            {
                // Continue to final review.
                await Shell.Current.Navigation.PushAsync(
                    new ReviewSubmitPage(_job));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Review navigation error: {ex}");

                ShowError(
                    "Your signatures were saved. Tap Continue again to open the review page.");
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    // =========================================================
    // UPLOAD ONE SIGNATURE (ONLINE)
    // =========================================================

    private async Task UploadSignatureAsync(
        string filePath,
        string signerName,
        string type)
    {
        await _apiService.UploadSignatureFileAsync(
            _job.ApiId,
            filePath,
            signerName,
            type);
    }

    // =========================================================
    // SAVE ONE SIGNATURE FOR LATER SYNC (OFFLINE)
    // =========================================================

    private async Task SaveSignatureForLaterSyncAsync(
        string sourceFilePath,
        string signerName,
        string type)
    {
        // The drawing pad wrote its PNG to the OS cache folder, which can
        // be cleared under storage pressure - copy it into persistent app
        // storage so it survives until it can actually be uploaded.
        string localFilePath = GetLocalSignatureFilePath(type);

        File.Copy(sourceFilePath, localFilePath, overwrite: true);

        // Only one pending copy per signature slot - replace whatever was
        // queued before (e.g. from an earlier offline attempt).
        await RemovePendingSyncQueueItemAsync(type);

        SignatureSyncPayload payload = new SignatureSyncPayload
        {
            Type = type,
            SignerName = signerName,
            LocalFilePath = localFilePath
        };

        await _localDatabaseService.AddSyncQueueItemAsync(
            new SyncQueueItem
            {
                JobId = _job.ApiId.ToString(),
                OperationType = "UploadSignature",
                PayloadJson = JsonSerializer.Serialize(payload)
            });
    }

    // =========================================================
    // OFFLINE SIGNATURE HELPERS
    // =========================================================

    private string GetLocalSignatureFilePath(string type) =>
        Path.Combine(
            FileSystem.AppDataDirectory,
            $"signature_{_job.ApiId}_{type}.png");

    private async Task<(SignatureSyncPayload? Technician, SignatureSyncPayload? Client)>
        LoadPendingSignaturePayloadsAsync()
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        SignatureSyncPayload? technician = null;
        SignatureSyncPayload? client = null;

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals("UploadSignature", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SignatureSyncPayload? payload = TryParseSignaturePayload(item.PayloadJson);
            if (payload is null) continue;

            if (payload.Type == "Technician") technician = payload;
            else if (payload.Type == "Client") client = payload;
        }

        return (technician, client);
    }

    private async Task RemovePendingSyncQueueItemAsync(string type)
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals("UploadSignature", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SignatureSyncPayload? payload = TryParseSignaturePayload(item.PayloadJson);
            if (payload?.Type == type)
            {
                await _localDatabaseService.DeleteSyncQueueItemAsync(item);
            }
        }
    }

    private async Task ClearPendingSignatureAsync(string type)
    {
        await RemovePendingSyncQueueItemAsync(type);

        string path = GetLocalSignatureFilePath(type);
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Could not delete local signature file: {ex}");
            }
        }
    }

    private static SignatureSyncPayload? TryParseSignaturePayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SignatureSyncPayload>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    // =========================================================
    // ERROR HANDLING
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
// SIGNATURE SYNC PAYLOAD
//
// What gets stored in a SyncQueueItem.PayloadJson for a signature
// that was captured offline and is waiting to be uploaded. Read
// back here when reloading the Signature screen, and read back by
// OfflineSyncViewModel when it actually uploads it.
// =============================================================

public class SignatureSyncPayload
{
    public string Type { get; set; } = string.Empty;

    public string SignerName { get; set; } = string.Empty;

    public string LocalFilePath { get; set; } = string.Empty;
}