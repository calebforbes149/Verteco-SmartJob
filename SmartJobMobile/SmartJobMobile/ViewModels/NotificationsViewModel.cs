using System.Collections.ObjectModel;
using SmartJobMobile.Models;
using SmartJobMobile.Services;

namespace SmartJobMobile.ViewModels;

public class NotificationsViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    public NotificationsViewModel(ApiService apiService)
    {
        _apiService = apiService;

        _ = LoadNotificationsAsync();
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
}