using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class ReviewSubmitViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;

    private bool _isSubmissionConfirmed;
    private bool _isSubmitting;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    // =========================================================
    // JOB INFORMATION
    // =========================================================

    public string JobNumber => _job.JobNumber;

    public string JobTitle => _job.Title;

    public string CustomerName => _job.CustomerName;

    public string JobLocation => _job.Location;

    public string Priority => _job.Priority;

    public string Status => _job.Status;

    public string ScheduledDate =>
        _job.ScheduledDateDisplay;

    // =========================================================
    // SUBMISSION CONFIRMATION
    // =========================================================

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

    // =========================================================
    // PAGE STATE
    // =========================================================

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

    // =========================================================
    // COMMANDS
    // =========================================================

    public ICommand SubmitCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ReviewSubmitViewModel(
        Job job,
        ApiService apiService,
        ConnectivityService connectivityService)
    {
        _job = job;
        _apiService = apiService;
        _connectivityService = connectivityService;

        SubmitCommand =
            new Command(
                async () => await SubmitAsync());
    }

    // =========================================================
    // SUBMIT JOB
    // =========================================================

    private async Task SubmitAsync()
    {
        ClearError();

        if (_job.ApiId == Guid.Empty)
        {
            ShowError(
                "SmartJob could not identify the current job.");

            return;
        }

        if (!IsSubmissionConfirmed)
        {
            ShowError(
                "Please confirm that you have reviewed the job before submitting.");

            return;
        }

        if (!_connectivityService.IsOnline)
        {
            ShowError(
                "Submitting a completed job currently requires an internet connection. Please reconnect and try again.");

            return;
        }

        if (IsSubmitting)
        {
            return;
        }

        try
        {
            IsSubmitting = true;

            bool submitted =
                await _apiService.SubmitJobAsync(
                    _job.ApiId);

            if (!submitted)
            {
                ShowError(
                    "The job could not be submitted. Please try again.");

                return;
            }

            _job.Status = "Submitted";

            await Shell.Current.Navigation.PushAsync(
                new JobSubmittedPage(_job));
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Submit Job Network Error: {ex}");

            ShowError(
                "SmartJob could not submit the job. Please check the connection and try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Submit Job Error: {ex}");

            ShowError(
                "The job could not be submitted. Please make sure all required job information has been completed.");
        }
        finally
        {
            IsSubmitting = false;
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