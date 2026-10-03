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
    }

    // =========================================================
    // SUBMIT DRAWN SIGNATURES
    // =========================================================

    public async Task SubmitDrawnSignaturesAsync(
        string technicianSignaturePath,
        string clientSignaturePath)
    {
        ClearError();

        // Make sure this is a real backend job.
        if (_job.ApiId == Guid.Empty)
        {
            ShowError(
                "SmartJob could not identify the current job.");

            return;
        }

        // Technician name.
        if (string.IsNullOrWhiteSpace(TechnicianName))
        {
            ShowError(
                "Please enter the technician name.");

            return;
        }

        // Technician confirmation.
        if (!TechnicianConfirmed)
        {
            ShowError(
                "The technician must confirm the job information.");

            return;
        }

        // Technician signature file.
        if (string.IsNullOrWhiteSpace(
                technicianSignaturePath) ||
            !File.Exists(
                technicianSignaturePath))
        {
            ShowError(
                "Please provide the technician signature.");

            return;
        }

        // Client name.
        if (string.IsNullOrWhiteSpace(ClientName))
        {
            ShowError(
                "Please enter the client name.");

            return;
        }

        // Client confirmation.
        if (!ClientConfirmed)
        {
            ShowError(
                "The client must confirm the completed work.");

            return;
        }

        // Client signature file.
        if (string.IsNullOrWhiteSpace(
                clientSignaturePath) ||
            !File.Exists(
                clientSignaturePath))
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

            // Upload technician signature.
            await UploadSignatureAsync(
                technicianSignaturePath,
                TechnicianName.Trim(),
                "Technician");

            // Upload client signature.
            await UploadSignatureAsync(
                clientSignaturePath,
                ClientName.Trim(),
                "Client");

            // Continue to final review.
            await Shell.Current.Navigation.PushAsync(
                new ReviewSubmitPage(_job));
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Signature Network Error: {ex}");

            ShowError(
                "SmartJob could not upload the signatures. Please check the connection and try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Signature Save Error: {ex}");

            ShowError(
                "The signatures could not be saved. Please try again.");
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
                .CreateEvidenceUploadAsync(
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