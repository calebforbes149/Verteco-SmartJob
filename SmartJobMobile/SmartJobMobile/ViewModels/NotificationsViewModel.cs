using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartJobMobile.Models;
using SmartJobMobile.Services;

namespace SmartJobMobile.ViewModels;

public class NotificationsViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;

    private bool _isMarkingAllRead;

    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    // Every notification SmartJob has ever sent for this technician.
    public int TotalCount =>
        Notifications.Count;

    // Only the ones that haven't been opened yet - this is what the
    // Dashboard's bell badge actually reflects, and what "new
    // notification" means on this screen.
    public int UnreadCount =>
        Notifications.Count(n => !n.IsRead);

    public bool HasUnread =>
        UnreadCount > 0;

    public bool HasNotifications =>
        Notifications.Count > 0;

    public bool IsMarkingAllRead
    {
        get => _isMarkingAllRead;
        private set
        {
            if (_isMarkingAllRead == value) return;
            _isMarkingAllRead = value;
            OnPropertyChanged();
        }
    }

    public ICommand MarkAllAsReadCommand { get; }

    public NotificationsViewModel(ApiService apiService)
    {
        _apiService = apiService;

        Notifications.CollectionChanged += Notifications_CollectionChanged;

        MarkAllAsReadCommand =
            new Command(
                async () =>
                    await MarkAllAsReadAsync());

        _ = LoadNotificationsAsync();
    }

    private void Notifications_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (NotificationItem item in e.NewItems)
                item.PropertyChanged += NotificationItem_PropertyChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (NotificationItem item in e.OldItems)
                item.PropertyChanged -= NotificationItem_PropertyChanged;
        }

        RaiseCountsChanged();
    }

    private void NotificationItem_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        // A single item flipping from unread to read changes the
        // unread total shown at the top of this screen - without this,
        // that summary would only ever reflect the state at load time.
        if (e.PropertyName == nameof(NotificationItem.IsRead))
            RaiseCountsChanged();
    }

    private void RaiseCountsChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasUnread));
        OnPropertyChanged(nameof(HasNotifications));
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            MobileNotificationsResponse? response =
                await _apiService.GetNotificationsAsync();

            if (response == null)
                return;

            Notifications.Clear();

            foreach (ApiNotification apiNotification in response.Items)
            {
                Notifications.Add(new NotificationItem
                {
                    ApiId = apiNotification.Id,
                    Title = apiNotification.Title,
                    Message = apiNotification.Message,
                    Type = apiNotification.Type,
                    IsRead = apiNotification.IsRead,
                    CreatedAtUtc = apiNotification.CreatedAtUtc
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to load SmartJob notifications: {ex.Message}");

            await Shell.Current.DisplayAlert(
                "Unable to Load Notifications",
                "SmartJob could not retrieve your notifications from the server.",
                "OK");
        }
    }

    public async Task MarkAsReadAsync(NotificationItem notification)
    {
        if (notification.IsRead) return;

        try
        {
            await _apiService.MarkNotificationReadAsync(notification.ApiId);
            notification.IsRead = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to mark notification read: {ex.Message}");
        }
    }

    private async Task MarkAllAsReadAsync()
    {
        if (IsMarkingAllRead) return;

        NotificationItem[] unread =
            Notifications.Where(n => !n.IsRead).ToArray();

        if (unread.Length == 0) return;

        try
        {
            IsMarkingAllRead = true;

            foreach (NotificationItem notification in unread)
                await MarkAsReadAsync(notification);
        }
        finally
        {
            IsMarkingAllRead = false;
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
