using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class CommentsFlagViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDatabaseService;

    private string _comments = string.Empty;
    private bool _isFlagged;
    private string? _selectedFlagReason;
    private string _flagDetails = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;

    public string Comments
    {
        get => _comments;
        set
        {
            _comments = value;
            OnPropertyChanged();
        }
    }

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

    public CommentsFlagViewModel(
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
        // If the job is flagged, a reason and
        // description of the problem are required.
        if (IsFlagged)
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
        }

        if (IsSaving)
        {
            return;
        }

        try
        {
            ClearError();
            IsSaving = true;

            var commentsFlagData = new
            {
                JobId = "JOB-1001",

                Comments =
                    Comments?.Trim()
                    ?? string.Empty,

                IsFlagged =
                    IsFlagged,

                FlagReason =
                    IsFlagged
                        ? SelectedFlagReason
                            ?? string.Empty
                        : string.Empty,

                FlagDetails =
                    IsFlagged
                        ? FlagDetails?.Trim()
                            ?? string.Empty
                        : string.Empty,

                SavedAt =
                    DateTime.UtcNow
            };

            string payloadJson =
                JsonSerializer.Serialize(
                    commentsFlagData);

            SyncQueueItem queueItem =
                new SyncQueueItem
                {
                    JobId = "JOB-1001",

                    OperationType =
                        "UpdateCommentsFlag",

                    PayloadJson =
                        payloadJson,

                    // The real RowVersion will be
                    // supplied by the API later.
                    BaseRowVersion =
                        string.Empty
                };

            await _localDatabaseService
                .AddSyncQueueItemAsync(
                    queueItem);

            await Shell.Current.GoToAsync(
                nameof(SignaturePage));
        }
        catch (Exception)
        {
            ShowError(
                "The comments and flag information could not be saved locally. Please try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}