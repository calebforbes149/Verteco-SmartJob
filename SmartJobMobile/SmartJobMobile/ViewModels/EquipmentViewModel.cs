using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        !IsSaving;

    public ICommand ContinueCommand { get; }

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
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private async Task ContinueAsync()
    {
        ClearError();

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

        if (IsSaving)
        {
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

        // -----------------------------------------------------
        // EQUIPMENT CURRENTLY REQUIRES CONNECTION
        // -----------------------------------------------------

        if (!_connectivityService.IsOnline)
        {
            ShowError(
                "Equipment details currently require an internet connection. Please reconnect before continuing.");

            return;
        }

        try
        {
            IsSaving =
                true;

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