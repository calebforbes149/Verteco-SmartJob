using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartJobMobile.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private string _technicianName = "Technician";
    private int _assignedJobs = 5;
    private int _inProgressJobs = 2;
    private int _completedJobs = 3;
    private int _notificationCount = 2;
    private string _connectionStatus = "Online";

    public string TechnicianName
    {
        get => _technicianName;
        set
        {
            _technicianName = value;
            OnPropertyChanged();
        }
    }

    public int AssignedJobs
    {
        get => _assignedJobs;
        set
        {
            _assignedJobs = value;
            OnPropertyChanged();
        }
    }

    public int InProgressJobs
    {
        get => _inProgressJobs;
        set
        {
            _inProgressJobs = value;
            OnPropertyChanged();
        }
    }

    public int CompletedJobs
    {
        get => _completedJobs;
        set
        {
            _completedJobs = value;
            OnPropertyChanged();
        }
    }

    public int NotificationCount
    {
        get => _notificationCount;
        set
        {
            _notificationCount = value;
            OnPropertyChanged();
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            _connectionStatus = value;
            OnPropertyChanged();
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