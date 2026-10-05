using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
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
    private bool _isLoading;
    private bool _hasSavedEquipment;
    private bool _savedDataLoaded;
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
        if (!_connectivityService.IsOnline || _job.ApiId == Guid.Empty)
        {
            ShowError("Connect to the API to load saved equipment before editing this job.");
            return;
        }
        try
        {
            IsLoading = true;
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
                EquipmentName = detail.FieldEquipmentName ?? string.Empty;
                SerialNumber = detail.FieldEquipmentSerialNumber;
                ReadDescription(detail.FieldEquipmentDescription);
                HasSavedEquipment = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Equipment load failed: {ex}");
            ShowError("Saved equipment details could not be loaded. Please reconnect and try again.");
        }
        finally { IsLoading = false; }
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
            if (HasSavedEquipment)
                await _apiService.RemoveEquipmentAsync(_job.ApiId);
            HasSavedEquipment = false;
            SelectedSiteEquipment = null;
            EquipmentName = SerialNumber = AssetNumber = ModelNumber = EquipmentNotes = string.Empty;
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

            HasSavedEquipment = true;

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