using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class SignatureViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDatabaseService;

    private string _technicianName = string.Empty;
    private string _clientName = string.Empty;

    private bool _technicianConfirmed;
    private bool _clientConfirmed;

    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;

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

    public SignatureViewModel(
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
        if (string.IsNullOrWhiteSpace(TechnicianName))
        {
            ShowError(
                "Please enter the technician name.");

            return;
        }

        if (!TechnicianConfirmed)
        {
            ShowError(
                "The technician must confirm the job information.");

            return;
        }

        if (string.IsNullOrWhiteSpace(ClientName))
        {
            ShowError(
                "Please enter the client name.");

            return;
        }

        if (!ClientConfirmed)
        {
            ShowError(
                "The client must confirm the completed work.");

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

            var signatureData = new
            {
                JobId = "JOB-1001",

                TechnicianName =
                    TechnicianName.Trim(),

                TechnicianConfirmed =
                    TechnicianConfirmed,

                ClientName =
                    ClientName.Trim(),

                ClientConfirmed =
                    ClientConfirmed,

                ConfirmedAt =
                    DateTime.UtcNow
            };

            string payloadJson =
                JsonSerializer.Serialize(
                    signatureData);

            SyncQueueItem queueItem =
                new SyncQueueItem
                {
                    JobId = "JOB-1001",

                    OperationType =
                        "UpdateSignatures",

                    PayloadJson =
                        payloadJson,

                    // Temporary until we receive
                    // the real RowVersion from the API.
                    BaseRowVersion =
                        string.Empty
                };

            await _localDatabaseService
                .AddSyncQueueItemAsync(
                    queueItem);

            await Shell.Current.GoToAsync(
                nameof(ReviewSubmitPage));
        }
        catch (Exception)
        {
            ShowError(
                "The signature information could not be saved locally. Please try again.");
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