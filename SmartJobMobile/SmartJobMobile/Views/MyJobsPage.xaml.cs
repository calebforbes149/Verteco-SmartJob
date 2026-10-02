using SmartJobMobile.Models;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class MyJobsPage : ContentPage
{
    public MyJobsPage()
    {
        InitializeComponent();

        BindingContext = new MyJobsViewModel();
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void ViewJobButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is Job selectedJob)
        {
            await Navigation.PushAsync(
                new JobDetailsPage(selectedJob));
        }
    }
}