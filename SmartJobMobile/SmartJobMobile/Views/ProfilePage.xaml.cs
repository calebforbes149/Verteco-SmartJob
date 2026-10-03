using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();

        BindingContext =
            new ProfileViewModel();
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OfflineSync_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new OfflineSyncPage());
    }
}