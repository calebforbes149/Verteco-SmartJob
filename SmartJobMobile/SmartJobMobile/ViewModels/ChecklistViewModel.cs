using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class ChecklistViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly LocalDatabaseService _localDatabaseService;
    private readonly ApiService _apiService;

    private string _technicianNotes = string.Empty;
    private int _completedCount;
    private bool _isLoading;
    private bool _isSaving;

    // =========================================================
    // JOB INFORMATION
    // =========================================================

    public string JobNumber => _job.JobNumber;

    public string JobTitle => _job.Title;

    public string JobLocation => _job.Location;

    // =========================================================
    // CHECKLIST ITEMS
    // =========================================================

    public ObservableCollection<ChecklistItemViewModel>
        ChecklistItems
    { get; } = new();

    // =========================================================
    // TECHNICIAN NOTES
    // =========================================================

    public string TechnicianNotes
    {
        get => _technicianNotes;
        set
        {
            if (_technicianNotes == value)
            {
                return;
            }

            _technicianNotes = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // PROGRESS
    // =========================================================

    public int CompletedCount
    {
        get => _completedCount;
        private set
        {
            if (_completedCount == value)
            {
                return;
            }

            _completedCount = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(Progress));
        }
    }

    public int TotalCount =>
        ChecklistItems.Count;

    public string ProgressText =>
        $"{CompletedCount} / {TotalCount}";

    public double Progress
    {
        get
        {
            if (TotalCount == 0)
            {
                return 0;
            }

            return (double)CompletedCount /
                   TotalCount;
        }
    }

    // =========================================================
    // LOADING / SAVING
    // =========================================================

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanContinue));
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (_isSaving == value)
            {
                return;
            }

            _isSaving = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanContinue));
        }
    }

    public bool CanContinue =>
        !IsLoading &&
        !IsSaving &&
        TotalCount > 0;

    // =========================================================
    // COMMAND
    // =========================================================

    public ICommand ContinueCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ChecklistViewModel(
        Job job,
        LocalDatabaseService localDatabaseService,
        ApiService apiService)
    {
        _job = job;
        _localDatabaseService = localDatabaseService;
        _apiService = apiService;

        ContinueCommand =
            new Command(
                async () => await ContinueAsync());

        _ = LoadChecklistAsync();
    }

    // =========================================================
    // LOAD REAL CHECKLIST FROM API
    // =========================================================

    private async Task LoadChecklistAsync()
    {
        if (_job.ApiId == Guid.Empty)
        {
            await Shell.Current.DisplayAlert(
                "Invalid Job",
                "This job does not have a valid server ID.",
                "OK");

            return;
        }

        try
        {
            IsLoading = true;

            ApiJobDetail? detail =
                await _apiService.GetJobDetailsAsync(
                    _job.ApiId);

            if (detail == null)
            {
                await Shell.Current.DisplayAlert(
                    "Checklist Unavailable",
                    "The checklist could not be retrieved from the server.",
                    "OK");

                return;
            }

            ChecklistItems.Clear();

            foreach (ApiChecklistItem apiItem
                     in detail.ChecklistItems
                         .OrderBy(item => item.Order))
            {
                ChecklistItemViewModel item =
                    new ChecklistItemViewModel
                    {
                        Id = apiItem.Id,
                        Order = apiItem.Order,
                        Text = apiItem.Text
                    };

                // Set the existing server value BEFORE
                // subscribing to changes.
                item.SetCompletedFromServer(
                    apiItem.IsDone);

                item.PropertyChanged +=
                    ChecklistItem_PropertyChanged;

                ChecklistItems.Add(item);
            }

            UpdateProgress();

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(CanContinue));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to load checklist: {ex.Message}");

            await Shell.Current.DisplayAlert(
                "Unable to Load Checklist",
                "SmartJob could not retrieve the checklist from the server.",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // =========================================================
    // CHECKBOX CHANGED
    // =========================================================

    private async void ChecklistItem_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
            nameof(ChecklistItemViewModel.IsCompleted))
        {
            return;
        }

        if (sender is not ChecklistItemViewModel item)
        {
            return;
        }

        // Update the progress bar immediately.
        UpdateProgress();

        // Do not send another request while this
        // checklist item is already being updated.
        if (item.IsUpdating)
        {
            return;
        }

        bool requestedValue = item.IsCompleted;
        bool previousValue = !requestedValue;

        try
        {
            item.IsUpdating = true;

            ApiJobDetail? updatedJob =
                await _apiService.ToggleChecklistItemAsync(
                    _job.ApiId,
                    item.Id);

            if (updatedJob == null)
            {
                item.SetCompletedFromServer(
                    previousValue);

                UpdateProgress();

                await Shell.Current.DisplayAlert(
                    "Update Failed",
                    "The checklist item could not be updated.",
                    "OK");

                return;
            }

            ApiChecklistItem? serverItem =
                updatedJob.ChecklistItems
                    .FirstOrDefault(
                        checklistItem =>
                            checklistItem.Id == item.Id);

            if (serverItem == null)
            {
                item.SetCompletedFromServer(
                    previousValue);

                UpdateProgress();

                await Shell.Current.DisplayAlert(
                    "Update Failed",
                    "The checklist item could not be confirmed by the server.",
                    "OK");

                return;
            }

            // The API response is the source of truth.
            item.SetCompletedFromServer(
                serverItem.IsDone);

            UpdateProgress();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Checklist update failed: {ex.Message}");

            // Put the checkbox back to its previous
            // state if the server update failed.
            item.SetCompletedFromServer(
                previousValue);

            UpdateProgress();

            await Shell.Current.DisplayAlert(
                "Unable to Update Checklist",
                "The checklist change could not be saved to the server. Your previous value has been restored.",
                "OK");
        }
        finally
        {
            item.IsUpdating = false;
        }
    }

    // =========================================================
    // UPDATE PROGRESS
    // =========================================================

    private void UpdateProgress()
    {
        CompletedCount =
            ChecklistItems.Count(
                item => item.IsCompleted);

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(CanContinue));
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private async Task ContinueAsync()
    {
        if (TotalCount == 0)
        {
            await Shell.Current.DisplayAlert(
                "Checklist Unavailable",
                "There are no checklist items available for this job.",
                "OK");

            return;
        }

        if (CompletedCount < TotalCount)
        {
            await Shell.Current.DisplayAlert(
                "Checklist Incomplete",
                "Please tick all checklist items before continuing.",
                "OK");

            return;
        }

        if (IsSaving)
        {
            return;
        }

        try
        {
            IsSaving = true;

            var checklistData = new
            {
                JobId = _job.ApiId,

                JobNumber = _job.JobNumber,

                TechnicianNotes =
                    TechnicianNotes,

                Items =
                    ChecklistItems.Select(
                        item => new
                        {
                            ChecklistItemId =
                                item.Id,

                            item.Order,

                            item.Text,

                            IsDone =
                                item.IsCompleted
                        })
                    .ToList(),

                CompletedAt =
                    DateTime.UtcNow
            };

            string payloadJson =
                JsonSerializer.Serialize(
                    checklistData);

            SyncQueueItem queueItem =
                new SyncQueueItem
                {
                    JobId =
                        _job.ApiId.ToString(),

                    OperationType =
                        "UpdateChecklist",

                    PayloadJson =
                        payloadJson,

                    BaseRowVersion =
                        string.Empty
                };

            await _localDatabaseService
                .AddSyncQueueItemAsync(
                    queueItem);

            await Shell.Current.GoToAsync(
                nameof(EquipmentPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to save checklist: {ex.Message}");

            await Shell.Current.DisplayAlert(
                "Save Error",
                "The checklist could not be saved locally. Please try again.",
                "OK");
        }
        finally
        {
            IsSaving = false;
        }
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


// =============================================================
// INDIVIDUAL CHECKLIST ITEM
// =============================================================

public class ChecklistItemViewModel :
    INotifyPropertyChanged
{
    private bool _isCompleted;
    private bool _isUpdating;

    public Guid Id { get; set; }

    public int Order { get; set; }

    public string Text { get; set; } =
        string.Empty;

    // API order starts at 0.
    // User-facing numbering starts at 1.
    public string DisplayText =>
        $"{Order + 1}. {Text}";

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted == value)
            {
                return;
            }

            _isCompleted = value;

            OnPropertyChanged();
        }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        set
        {
            if (_isUpdating == value)
            {
                return;
            }

            _isUpdating = value;

            OnPropertyChanged();
        }
    }

    public void SetCompletedFromServer(
        bool value)
    {
        _isCompleted = value;

        OnPropertyChanged(
            nameof(IsCompleted));
    }

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