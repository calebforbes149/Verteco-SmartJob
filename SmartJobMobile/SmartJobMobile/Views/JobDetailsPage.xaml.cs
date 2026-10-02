using SmartJobMobile.Models;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class JobDetailsPage : ContentPage
{
    public JobDetailsPage(Job job)
    {
        InitializeComponent();

        BindingContext = new JobDetailsViewModel(job);
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}