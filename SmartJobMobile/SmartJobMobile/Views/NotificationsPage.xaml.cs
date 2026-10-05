using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class NotificationsPage : ContentPage
{
    private NotificationsViewModel ViewModel =>
        (NotificationsViewModel)BindingContext;

    public NotificationsPage()
    {
        InitializeComponent();

        ApiService apiService =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<ApiService>();

        BindingContext =
            new NotificationsViewModel(apiService);
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void Notification_Tapped(
        object sender,
        EventArgs e)
    {
        if (sender is Border border &&
            border.BindingContext is NotificationItem notification)
        {
            await ViewModel.MarkAsReadAsync(notification);
        }
    }
}