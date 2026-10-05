using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class CommentsFlagViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;

    private string _comments = string.Empty;
    private bool _isFlagged;
    private string? _selectedFlagReason;
    private string _flagDetails = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;
    private bool _isLoading;
    private bool _commentsTouched;
    private bool _alreadyFlagged;
    private bool _savedDataLoaded;

    public bool CanChangeFlag => !_alreadyFlagged;

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanContinue)); }
    }

    // =========================================================
    // REAL JOB INFORMATION
    // =========================================================

    public string JobNumber => _job.JobNumber;

    public string JobTitle => _job.Title;

    public string JobLocation => _job.Location;

    // =========================================================
    // COMMENTS
    // =========================================================

    public string Comments
    {
        get => _comments;
        set
        {
            if (_comments == value) return;
            _comments = value;
            _commentsTouched = true;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // FLAG JOB
    // =========================================================

    public bool IsFlagged
    {
        get => _isFlagged;
        set
        {
            _isFlagged = value;
            OnPropertyChanged();

            if (!value)
            {
                SelectedFlagReason = null;
                FlagDetails = string.Empty;
            }

            ClearError();
        }
    }

    public ObservableCollection<string> FlagReasons { get; } =
        new()
        {
            "Equipment damaged",
            "Safety concern",
            "Additional repair required",
            "Replacement part required",
            "Unable to complete job",
            "Client issue",
            "Other"
        };

    public string? SelectedFlagReason
    {
        get => _selectedFlagReason;
        set
        {
            _selectedFlagReason = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public string FlagDetails
    {
        get => _flagDetails;
        set
        {
            _flagDetails = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    // =========================================================
    // ERROR STATE
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

    // =========================================================
    // SAVING STATE
    // =========================================================

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

    public bool CanContinue => !IsSaving && !IsLoading && _savedDataLoaded;

    public ICommand ContinueCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CommentsFlagViewModel(
        Job job,
        ApiService apiService,
        ConnectivityService connectivityService)
    {
        _job = job;
        _apiService = apiService;
        _connectivityService = connectivityService;

        ContinueCommand =
            new Command(
                async () => await ContinueAsync());

        _ = LoadSavedWorkAsync();
    }

    private async Task LoadSavedWorkAsync()
    {
        if (!_connectivityService.IsOnline || _job.ApiId == Guid.Empty)
        {
            ShowError("Connect to the API to load saved comments before editing this job.");
            return;
        }
        try
        {
            IsLoading = true;
            var detail = await _apiService.GetJobDetailsAsync(_job.ApiId);
            if (detail is null) return;
            _savedDataLoaded = true;
            OnPropertyChanged(nameof(CanContinue));
            if (!_commentsTouched)
            {
                _comments = detail.TechnicianComments ?? string.Empty;
                OnPropertyChanged(nameof(Comments));
            }
            _alreadyFlagged = detail.IsFlagged;
            OnPropertyChanged(nameof(CanChangeFlag));
            if (_alreadyFlagged)
            {
                IsFlagged = true;
                var reason = detail.FlagReason ?? string.Empty;
                SelectedFlagReason = FlagReasons.FirstOrDefault(option =>
                    reason.StartsWith(option + ":", StringComparison.OrdinalIgnoreCase)) ?? "Other";
                FlagDetails = reason.Contains(':') ? reason[(reason.IndexOf(':') + 1)..].Trim() : reason;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Saved comments load failed: {ex}");
            ShowError("Saved comments could not be loaded. Please reconnect and try again.");
        }
        finally { IsLoading = false; }
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private async Task ContinueAsync()
    {
        ClearError();

        if (IsLoading || IsSaving) return;
        if (!_savedDataLoaded)
        {
            ShowError("Saved comments have not loaded. Reopen this screen while connected to the API.");
            return;
        }

        if (_job.ApiId == Guid.Empty)
        {
            ShowError(
                "SmartJob could not identify the current job.");

            return;
        }

        // A reason and description are required
        // when the technician flags the job.
        if (IsFlagged && !_alreadyFlagged)
        {
            if (string.IsNullOrWhiteSpace(
                SelectedFlagReason))
            {
                ShowError(
                    "Please select a reason for flagging this job.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                FlagDetails))
            {
                ShowError(
                    "Please describe the issue that requires attention.");

                return;
            }

            if (!_connectivityService.IsOnline)
            {
                ShowError(
                    "Flagging a job currently requires an internet connection. Please reconnect before continuing.");

                return;
            }
        }

        if (_commentsTouched && !_connectivityService.IsOnline)
        {
            ShowError("Saving technician comments requires an internet connection. Please reconnect and try again.");
            return;
        }

        try
        {
            IsSaving = true;

            // Only call the backend when the technician
            // actually selected Flag this job.
            if (_commentsTouched)
            {
                await _apiService.SaveTechnicianCommentsAsync(_job.ApiId, Comments);
                _commentsTouched = false;
            }

            if (IsFlagged && !_alreadyFlagged)
            {
                string reason =
                    $"{SelectedFlagReason}: {FlagDetails.Trim()}";

                await _apiService.FlagJobAsync(
                    _job.ApiId,
                    reason);
                _alreadyFlagged = true;
                OnPropertyChanged(nameof(CanChangeFlag));
            }

            await Shell.Current.Navigation.PushAsync(
                new SignaturePage(_job));
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Flag job HTTP error: {ex}");

            ShowError(
                "SmartJob could not save the job flag. Please check the connection and try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Comments and flag error: {ex}");

            ShowError(
                "The comments and flag information could not be processed. Please try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    // =========================================================
    // ERROR HELPERS
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}