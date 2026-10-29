using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();

        IServiceProvider services =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        ApiService apiService =
            services.GetRequiredService<ApiService>();

        BindingContext =
            new DashboardViewModel(apiService);
    }

    // The technician comes back to this same Dashboard instance after
    // viewing (and marking read) notifications, or after working a
    // job - without this, the job counts and the notification badge
    // would just show whatever was true when the app first opened.
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DashboardViewModel viewModel)
        {
            _ = viewModel.RefreshAsync();
        }
    }

    private async void ViewJobsButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MyJobsPage));
    }

    private async void Notifications_Tapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(NotificationsPage));
    }

    private async void Profile_Tapped(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(ProfilePage));
    }

    private async void SyncButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(OfflineSyncPage));
    }
}