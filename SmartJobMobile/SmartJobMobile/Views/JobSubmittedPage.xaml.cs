namespace SmartJobMobile.Views;

public partial class JobSubmittedPage : ContentPage
{
    public JobSubmittedPage()
    {
        InitializeComponent();
    }

    private async void MyJobsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MyJobsPage));
    }

    private async void DashboardButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(DashboardPage));
    }
}