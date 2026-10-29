using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartJobMobile.Models;
using SmartJobMobile.Services;

namespace SmartJobMobile.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;

    private string _technicianName = "Technician";

    private int _assignedJobs;
    private int _inProgressJobs;
    private int _completedJobs;
    private int _notificationCount;

    private string _connectionStatus = "See Offline Sync";


    public DashboardViewModel(ApiService apiService)
    {
        _apiService = apiService;

        _ = LoadDashboardAsync();
    }


    // The Dashboard page instance is reused when the technician
    // navigates back to it (e.g. from Notifications), so the data
    // loaded in the constructor above goes stale - marking
    // notifications as read, or finishing a job, wouldn't be
    // reflected here until the app restarted. Call this whenever
    // the page reappears to keep it current.
    public Task RefreshAsync() =>
        LoadDashboardAsync();


    public string TechnicianName
    {
        get => _technicianName;
        set
        {
            if (_technicianName == value)
                return;

            _technicianName = value;
            OnPropertyChanged();
        }
    }


    public int AssignedJobs
    {
        get => _assignedJobs;
        set
        {
            if (_assignedJobs == value)
                return;

            _assignedJobs = value;
            OnPropertyChanged();
        }
    }


    public int InProgressJobs
    {
        get => _inProgressJobs;
        set
        {
            if (_inProgressJobs == value)
                return;

            _inProgressJobs = value;
            OnPropertyChanged();
        }
    }


    public int CompletedJobs
    {
        get => _completedJobs;
        set
        {
            if (_completedJobs == value)
                return;

            _completedJobs = value;
            OnPropertyChanged();
        }
    }


    public int NotificationCount
    {
        get => _notificationCount;
        set
        {
            if (_notificationCount == value)
                return;

            _notificationCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNotifications));
            OnPropertyChanged(nameof(NotificationCountDisplay));
        }
    }

    // Only show the red badge on the bell icon when there's
    // actually something unread - a badge that's always on
    // screen (even showing "0") just looks broken.
    public bool HasNotifications =>
        NotificationCount > 0;

    // Caps the badge text at "9+" so the circle stays a fixed,
    // readable size instead of stretching (or clipping) to fit
    // a wide number.
    public string NotificationCountDisplay =>
        NotificationCount > 9
            ? "9+"
            : NotificationCount.ToString();


    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            if (_connectionStatus == value)
                return;

            _connectionStatus = value;
            OnPropertyChanged();
        }
    }


    private async Task LoadDashboardAsync()
    {
        try
        {
            MyJobsResponse? response =
                await _apiService.GetMyJobsAsync();

            if (response == null)
            {
                ResetJobCounts();
                return;
            }

            List<ApiJobSummary> jobs =
                response.Items?.ToList()
                ?? new List<ApiJobSummary>();

            /*
             * AssignedJobs represents all jobs currently
             * returned to this technician by the mobile API.
             */
            AssignedJobs = jobs.Count;

            /*
             * Count jobs that are actively being worked on.
             */
            InProgressJobs = jobs.Count(job =>
                string.Equals(
                    job.Status,
                    "InProgress",
                    StringComparison.OrdinalIgnoreCase));

            /*
             * Count completed jobs.
             */
            CompletedJobs = jobs.Count(job =>
                string.Equals(
                    job.Status,
                    "Completed",
                    StringComparison.OrdinalIgnoreCase));

            await LoadNotificationCountAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to load dashboard data: {ex.Message}");

            ResetJobCounts();
        }
    }


    /*
     * The bell icon's badge should reflect real, unread
     * notifications from SmartJob - the same endpoint the
     * Notifications screen already uses. A failure here
     * (e.g. offline) shouldn't affect the job counts above,
     * so it's kept in its own try/catch.
     */
    private async Task LoadNotificationCountAsync()
    {
        try
        {
            MobileNotificationsResponse? response =
                await _apiService.GetNotificationsAsync();

            NotificationCount =
                response?.Items?.Count(n => !n.IsRead) ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to load notification count: {ex.Message}");

            NotificationCount = 0;
        }
    }


    private void ResetJobCounts()
    {
        AssignedJobs = 0;
        InProgressJobs = 0;
        CompletedJobs = 0;
        NotificationCount = 0;
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