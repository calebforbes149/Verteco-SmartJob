using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class JobDetailsViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;

    private Job _selectedJob;

    public Job SelectedJob
    {
        get => _selectedJob;
        set
        {
            _selectedJob = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StartButtonText));
        }
    }

    public ICommand StartJobCommand { get; }

    public string StartButtonText => SelectedJob.Status switch
    {
        "Reopened" => "RESUME REOPENED JOB",
        "In Progress" => "CONTINUE JOB",
        _ => "START JOB"
    };

    public JobDetailsViewModel(
        Job job,
        ApiService apiService)
    {
        _selectedJob = job;
        _apiService = apiService;

        StartJobCommand = new Command(
            async () => await StartJobAsync());

        _ = LoadJobDetailsAsync();
    }

    // =========================================================
    // LOAD FULL JOB DETAILS FROM API
    // =========================================================
    private async Task LoadJobDetailsAsync()
    {
        try
        {
            ApiJobDetail? detail =
                await _apiService.GetJobDetailsAsync(
                    SelectedJob.ApiId);

            if (detail == null)
            {
                return;
            }

            SelectedJob = new Job
            {
                ApiId = detail.Id,

                // Keep the existing local UI ID
                Id = SelectedJob.Id,

                JobNumber = detail.Number,

                Title = detail.TemplateName,

                CustomerName = detail.CustomerName,

                Location = detail.SiteName,

                Status = FormatStatus(
                    detail.Status),

                Priority = detail.Priority,

                ScheduledDate =
                    detail.ScheduledDate.ToDateTime(
                        TimeOnly.MinValue)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to load job details: {ex.Message}");

            await Shell.Current.DisplayAlertAsync(
                "Unable to Load Job",
                "The full job details could not be retrieved from the server.",
                "OK");
        }
    }

    // =========================================================
    // START / CONTINUE JOB
    // =========================================================
    private async Task StartJobAsync()
    {
        try
        {
            // -------------------------------------------------
            // JOB IS ALREADY IN PROGRESS
            // Continue the existing workflow.
            // -------------------------------------------------
            if (SelectedJob.Status == "In Progress")
            {
                await NavigateToChecklistAsync();
                return;
            }

            if (SelectedJob.Status == "Reopened")
            {
                bool restarted = await _apiService.StartJobAsync(SelectedJob.ApiId);
                if (!restarted)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Unable to Resume Job",
                        "SmartJob could not resume this reopened job. Please check your connection and try again.",
                        "OK");
                    return;
                }

                SelectedJob.Status = "In Progress";
                OnPropertyChanged(nameof(SelectedJob));
                OnPropertyChanged(nameof(StartButtonText));
                await NavigateToChecklistAsync();
                return;
            }

            // -------------------------------------------------
            // COMPLETED JOB
            // -------------------------------------------------
            if (SelectedJob.Status == "Completed")
            {
                await Shell.Current.DisplayAlertAsync(
                    "Job Completed",
                    "This job has already been completed and cannot be started again.",
                    "OK");

                return;
            }

            // -------------------------------------------------
            // AWAITING APPROVAL
            // -------------------------------------------------
            if (SelectedJob.Status == "Awaiting Approval")
            {
                await Shell.Current.DisplayAlertAsync(
                    "Awaiting Approval",
                    "This job has already been submitted and is awaiting manager approval.",
                    "OK");

                return;
            }

            // -------------------------------------------------
            // ASSIGNED JOB
            //
            // The backend requires:
            // Assigned -> Accepted -> In Progress
            // -------------------------------------------------
            if (SelectedJob.Status == "Assigned")
            {
                bool accepted =
                    await _apiService.AcceptJobAsync(
                        SelectedJob.ApiId);

                if (!accepted)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Unable to Accept Job",
                        "SmartJob could not accept this job. Please check your connection and try again.",
                        "OK");

                    return;
                }

                SelectedJob.Status = "Accepted";

                OnPropertyChanged(
                    nameof(SelectedJob));
            }

            // -------------------------------------------------
            // ACCEPTED JOB
            // Start it using the real backend API.
            // -------------------------------------------------
            if (SelectedJob.Status == "Accepted")
            {
                bool started =
                    await _apiService.StartJobAsync(
                        SelectedJob.ApiId);

                if (!started)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Unable to Start Job",
                        "The job was accepted, but SmartJob could not start it. Please try again.",
                        "OK");

                    return;
                }

                SelectedJob.Status = "In Progress";

                OnPropertyChanged(
                    nameof(SelectedJob));
                OnPropertyChanged(nameof(StartButtonText));

                await NavigateToChecklistAsync();

                return;
            }

            // -------------------------------------------------
            // ANY OTHER STATUS
            // -------------------------------------------------
            await Shell.Current.DisplayAlertAsync(
                "Job Cannot Be Started",
                $"This job currently has the status '{SelectedJob.Status}'.",
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to start job: {ex.Message}");

            await Shell.Current.DisplayAlertAsync(
                "Unable to Start Job",
                "SmartJob could not start this job. Please check your connection and try again.",
                "OK");
        }
    }

    // =========================================================
    // START FIELD WORK
    // =========================================================
    private async Task NavigateToChecklistAsync()
    {
        Window? window =
            Application.Current?
                .Windows
                .FirstOrDefault();

        if (window?.Page == null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Navigation Error",
                "SmartJob could not open the field workflow.",
                "OK");

            return;
        }

        await window.Page.Navigation.PushAsync(
            new EquipmentPage(
                SelectedJob));
    }

    // =========================================================
    // FORMAT API STATUS FOR DISPLAY
    // =========================================================
    private static string FormatStatus(
        string status)
    {
        return status switch
        {
            "InProgress" =>
                "In Progress",

            "AwaitingApproval" =>
                "Awaiting Approval",

            _ => status
        };
    }

    // =========================================================
    // PROPERTY CHANGED
    // =========================================================
    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}