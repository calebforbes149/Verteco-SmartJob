using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();

        BindingContext = new DashboardViewModel();
    }

    private async void ViewJobsButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyJobsPage));
    }

    private async void Notifications_Tapped(
    object sender,
    TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(NotificationsPage));
    }

    private async void Profile_Tapped(
    object sender,
    TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(ProfilePage));
    }
    private async void SyncButton_Clicked(
    object sender,
    EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(OfflineSyncPage));
    }
}