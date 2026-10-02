using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class EquipmentViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDatabaseService;

    private string _equipmentName = string.Empty;
    private string _serialNumber = string.Empty;
    private string _assetNumber = string.Empty;
    private string _modelNumber = string.Empty;
    private string _equipmentNotes = string.Empty;

    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;

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

    public bool CanContinue => !IsSaving;

    public ICommand ContinueCommand { get; }

    public EquipmentViewModel(
        LocalDatabaseService localDatabaseService)
    {
        _localDatabaseService =
            localDatabaseService;

        ContinueCommand =
            new Command(
                async () => await ContinueAsync());
    }

    private async Task ContinueAsync()
    {
        // Equipment name is required.
        if (string.IsNullOrWhiteSpace(EquipmentName))
        {
            ShowError(
                "Please enter the equipment name or type.");

            return;
        }

        // Serial number is required.
        if (string.IsNullOrWhiteSpace(SerialNumber))
        {
            ShowError(
                "Please enter the equipment serial number.");

            return;
        }

        if (IsSaving)
        {
            return;
        }

        try
        {
            ClearError();
            IsSaving = true;

            // Create the equipment information
            // that must eventually be sent to the API.
            var equipmentData = new
            {
                JobId = "JOB-1001",

                EquipmentName =
                    EquipmentName.Trim(),

                SerialNumber =
                    SerialNumber.Trim(),

                AssetNumber =
                    AssetNumber?.Trim() ?? string.Empty,

                ModelNumber =
                    ModelNumber?.Trim() ?? string.Empty,

                EquipmentNotes =
                    EquipmentNotes?.Trim() ?? string.Empty,

                SavedAt =
                    DateTime.UtcNow
            };

            // Convert the equipment information
            // into JSON for the offline queue.
            string payloadJson =
                JsonSerializer.Serialize(
                    equipmentData);

            // Create the real SQLite queue operation.
            SyncQueueItem queueItem =
                new SyncQueueItem
                {
                    JobId = "JOB-1001",

                    OperationType =
                        "UpdateEquipment",

                    PayloadJson =
                        payloadJson,

                    // The real RowVersion will come
                    // from Person 4's API later.
                    BaseRowVersion =
                        string.Empty
                };

            // Save locally before navigating.
            await _localDatabaseService
                .AddSyncQueueItemAsync(
                    queueItem);

            // Continue to Photo Evidence.
            await Shell.Current.GoToAsync(
                nameof(PhotoCapturePage));
        }
        catch (Exception)
        {
            ShowError(
                "The equipment details could not be saved locally. Please try again.");
        }
        finally
        {
            IsSaving = false;
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