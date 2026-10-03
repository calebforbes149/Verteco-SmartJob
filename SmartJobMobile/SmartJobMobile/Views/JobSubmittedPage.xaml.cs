using SmartJobMobile.Models;

namespace SmartJobMobile.Views;

public partial class JobSubmittedPage : ContentPage
{
    private readonly Job _job;

    public JobSubmittedPage(Job job)
    {
        InitializeComponent();

        _job = job;

        BindingContext = _job;
    }

    private async void MyJobsButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MyJobsPage));
    }

    private async void DashboardButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(DashboardPage));
    }
}