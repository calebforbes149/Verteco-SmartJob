using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class ChecklistViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDatabaseService;

    private string _technicianNotes = string.Empty;

    private bool _check1Completed;
    private bool _check2Completed;
    private bool _check3Completed;
    private bool _check4Completed;

    private int _completedCount;
    private bool _isSaving;

    public string TechnicianNotes
    {
        get => _technicianNotes;
        set
        {
            _technicianNotes = value;
            OnPropertyChanged();
        }
    }

    public bool Check1Completed
    {
        get => _check1Completed;
        set
        {
            if (_check1Completed == value)
            {
                return;
            }

            _check1Completed = value;
            OnPropertyChanged();
            UpdateProgress();
        }
    }

    public bool Check2Completed
    {
        get => _check2Completed;
        set
        {
            if (_check2Completed == value)
            {
                return;
            }

            _check2Completed = value;
            OnPropertyChanged();
            UpdateProgress();
        }
    }

    public bool Check3Completed
    {
        get => _check3Completed;
        set
        {
            if (_check3Completed == value)
            {
                return;
            }

            _check3Completed = value;
            OnPropertyChanged();
            UpdateProgress();
        }
    }

    public bool Check4Completed
    {
        get => _check4Completed;
        set
        {
            if (_check4Completed == value)
            {
                return;
            }

            _check4Completed = value;
            OnPropertyChanged();
            UpdateProgress();
        }
    }

    public int CompletedCount
    {
        get => _completedCount;
        private set
        {
            _completedCount = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(Progress));
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

    public string ProgressText =>
        $"{CompletedCount} / 4";

    public double Progress =>
        CompletedCount / 4.0;

    public ICommand ContinueCommand { get; }

    public ChecklistViewModel(
        LocalDatabaseService localDatabaseService)
    {
        _localDatabaseService =
            localDatabaseService;

        ContinueCommand =
            new Command(
                async () => await ContinueAsync());

        UpdateProgress();
    }

    private void UpdateProgress()
    {
        int count = 0;

        if (Check1Completed)
        {
            count++;
        }

        if (Check2Completed)
        {
            count++;
        }

        if (Check3Completed)
        {
            count++;
        }

        if (Check4Completed)
        {
            count++;
        }

        CompletedCount = count;
    }

    private async Task ContinueAsync()
    {
        if (CompletedCount < 4)
        {
            await Shell.Current.DisplayAlert(
                "Checklist Incomplete",
                "Please tick all four checklist items before continuing.",
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
                JobId = "JOB-1001",

                VisualEquipmentInspection =
                    Check1Completed,

                SafetyComponents =
                    Check2Completed,

                ElectricalConnections =
                    Check3Completed,

                OperationalTest =
                    Check4Completed,

                TechnicianNotes =
                    TechnicianNotes,

                CompletedAt =
                    DateTime.UtcNow
            };

            string payloadJson =
                JsonSerializer.Serialize(
                    checklistData);

            SyncQueueItem queueItem =
                new SyncQueueItem
                {
                    JobId = "JOB-1001",

                    OperationType =
                        "UpdateChecklist",

                    PayloadJson =
                        payloadJson,

                    // Temporary until the real API
                    // provides the job RowVersion.
                    BaseRowVersion =
                        string.Empty
                };

            await _localDatabaseService
                .AddSyncQueueItemAsync(
                    queueItem);

            await Shell.Current.GoToAsync(
                nameof(EquipmentPage));
        }
        catch (Exception)
        {
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}