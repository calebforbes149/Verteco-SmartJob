using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartJobMobile.ViewModels;

public class SignatureViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
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

    // Fired right after signatures are confirmed on the server, so the page
    // can clear the drawing pads before the user could tap Continue again.
    public event Action? SignaturesUploaded;

    public bool HasTechnicianSignature => _technicianSignatureId.HasValue;
    public bool HasClientSignature => _clientSignatureId.HasValue;

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
        _apiService = apiService;
        _connectivityService = connectivityService;
        _ = LoadExistingSignaturesAsync();
    }

    private async Task LoadExistingSignaturesAsync()
    {
        if (!_connectivityService.IsOnline || _job.ApiId == Guid.Empty)
        {
            ShowError("Connect to the API to load saved signatures before editing this job.");
            return;
        }
        try
        {
            IsLoadingExisting = true;
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
            OnPropertyChanged(nameof(HasTechnicianSignature));
            OnPropertyChanged(nameof(HasClientSignature));
            OnPropertyChanged(nameof(ShowTechnicianPad));
            OnPropertyChanged(nameof(ShowClientPad));
            if (technician is not null)
            {
                _savedTechnicianName = technician.SignerName;
                TechnicianName = technician.SignerName;
                try
                {
                    var bytes = await _apiService.GetSignatureFileAsync(_job.ApiId, technician.Id);
                    TechnicianSignaturePreview = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Technician signature preview unavailable: {ex}"); }
            }
            else
            {
                _savedTechnicianName = string.Empty;
            }
            if (client is not null)
            {
                _savedClientName = client.SignerName;
                ClientName = client.SignerName;
                try
                {
                    var bytes = await _apiService.GetSignatureFileAsync(_job.ApiId, client.Id);
                    ClientSignaturePreview = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Client signature preview unavailable: {ex}"); }
            }
            else
            {
                _savedClientName = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Saved signatures load failed: {ex}");
            ShowError("Saved signatures could not be loaded. Please reconnect and try again.");
        }
        finally { IsLoadingExisting = false; }
    }

    public async Task RemoveExistingSignatureAsync(string type)
    {
        var id = type == "Technician" ? _technicianSignatureId : _clientSignatureId;
        if (!id.HasValue || IsSaving) return;
        try
        {
            IsSaving = true;
            await _apiService.RemoveSignatureAsync(_job.ApiId, id.Value);
            if (type == "Technician")
            {
                _technicianSignatureId = null;
                TechnicianSignaturePreview = null;
                TechnicianConfirmed = false;
            }
            else
            {
                _clientSignatureId = null;
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

        // Signature upload currently requires the API.
        if (!_connectivityService.IsOnline)
        {
            ShowError(
                "Uploading signatures currently requires an internet connection. Please reconnect before continuing.");

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
                // Upload technician signature.
                if (technicianSignaturePath is not null)
                    await UploadSignatureAsync(
                        technicianSignaturePath,
                        TechnicianName.Trim(),
                        "Technician");

                // Upload client signature.
                if (clientSignaturePath is not null)
                    await UploadSignatureAsync(
                        clientSignaturePath,
                        ClientName.Trim(),
                        "Client");
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
    // UPLOAD ONE SIGNATURE
    // =========================================================

    private async Task UploadSignatureAsync(
        string filePath,
        string signerName,
        string type)
    {
        string extension =
            Path.GetExtension(filePath)
                .ToLowerInvariant();

        if (extension == ".jpeg")
        {
            extension = ".jpg";
        }

        if (extension != ".jpg" &&
            extension != ".png")
        {
            throw new InvalidOperationException(
                "The generated signature must be a JPG or PNG file.");
        }

        // Ask the backend for an upload location.
        MobileEvidenceUploadResponse upload =
            await _apiService
                .CreateSignatureUploadAsync(
                    _job.ApiId,
                    extension);

        // Upload the generated signature image.
        await _apiService
            .UploadEvidenceFileAsync(
                upload.UploadUrl,
                filePath);

        // Calculate the SHA-256 hash required by the backend.
        string sha256 =
            await _apiService
                .CalculateSha256Async(
                    filePath);

        // Confirm the uploaded file as a job signature.
        await _apiService
            .ConfirmSignatureAsync(
                _job.ApiId,
                upload.UploadToken,
                sha256,
                signerName,
                type);
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