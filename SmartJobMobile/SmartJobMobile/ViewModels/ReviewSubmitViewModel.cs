using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class ReviewSubmitViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDatabaseService;

    private bool _isSubmissionConfirmed;
    private bool _isSubmitting;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public bool IsSubmissionConfirmed
    {
        get => _isSubmissionConfirmed;
        set
        {
            _isSubmissionConfirmed = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public bool IsSubmitting
    {
        get => _isSubmitting;
        private set
        {
            _isSubmitting = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSubmit));
            OnPropertyChanged(nameof(SubmitButtonText));
        }
    }

    public bool CanSubmit => !IsSubmitting;

    public string SubmitButtonText =>
        IsSubmitting
            ? "SUBMITTING..."
            : "SUBMIT JOB";

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

    public ICommand SubmitCommand { get; }

    public ReviewSubmitViewModel(
        LocalDatabaseService localDatabaseService)
    {
        _localDatabaseService =
            localDatabaseService;

        SubmitCommand =
            new Command(
                async () => await SubmitAsync());
    }

    private async Task SubmitAsync()
    {
        if (!IsSubmissionConfirmed)
        {
            ShowError(
                "Please confirm that you have reviewed the job before submitting.");

            return;
        }

        if (IsSubmitting)
        {
            return;
        }

        try
        {
            ClearError();
            IsSubmitting = true;

            var submissionData = new
            {
                JobId = "JOB-1001",

                SubmissionConfirmed =
                    IsSubmissionConfirmed,

                SubmittedAt =
                    DateTime.UtcNow,

                Status =
                    "Submitted"
            };

            string payloadJson =
                JsonSerializer.Serialize(
                    submissionData);

            SyncQueueItem queueItem =
                new SyncQueueItem
                {
                    JobId = "JOB-1001",

                    OperationType =
                        "SubmitJob",

                    PayloadJson =
                        payloadJson,

                    // Temporary until Person 4's API
                    // supplies the real RowVersion.
                    BaseRowVersion =
                        string.Empty
                };

            await _localDatabaseService
                .AddSyncQueueItemAsync(
                    queueItem);

            await Shell.Current.GoToAsync(
                nameof(JobSubmittedPage));
        }
        catch (Exception)
        {
            ShowError(
                "The job could not be saved for submission. Please try again.");
        }
        finally
        {
            IsSubmitting = false;
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