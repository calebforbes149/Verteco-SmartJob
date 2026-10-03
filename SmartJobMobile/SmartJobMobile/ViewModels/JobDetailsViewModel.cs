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
        }
    }

    public ICommand StartJobCommand { get; }

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

            await Shell.Current.DisplayAlert(
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
            // Continue working on the existing job.
            // -------------------------------------------------
            if (SelectedJob.Status == "In Progress")
            {
                await Application.Current!
                    .MainPage!
                    .Navigation
                    .PushAsync(
                        new ChecklistPage(
                            SelectedJob));

                return;
            }

            // -------------------------------------------------
            // COMPLETED JOB
            // Completed jobs cannot be started again.
            // -------------------------------------------------
            if (SelectedJob.Status == "Completed")
            {
                await Shell.Current.DisplayAlert(
                    "Job Completed",
                    "This job has already been completed and cannot be started again.",
                    "OK");

                return;
            }

            // -------------------------------------------------
            // ONLY ACCEPTED JOBS CAN BE STARTED
            // -------------------------------------------------
            if (SelectedJob.Status != "Accepted")
            {
                await Shell.Current.DisplayAlert(
                    "Job Cannot Be Started",
                    $"This job currently has the status '{SelectedJob.Status}'.",
                    "OK");

                return;
            }

            // -------------------------------------------------
            // CALL REAL API START ENDPOINT
            // -------------------------------------------------
            bool started =
                await _apiService.StartJobAsync(
                    SelectedJob.ApiId);

            if (started)
            {
                SelectedJob.Status =
                    "In Progress";

                // Pass the REAL selected job to the checklist.
                await Application.Current!
                    .MainPage!
                    .Navigation
                    .PushAsync(
                        new ChecklistPage(
                            SelectedJob));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to start job: {ex.Message}");

            await Shell.Current.DisplayAlert(
                "Unable to Start Job",
                "SmartJob could not start this job. Please check your connection and try again.",
                "OK");
        }
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