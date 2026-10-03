using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class MyJobsPage : ContentPage
{
    public MyJobsPage()
    {
        InitializeComponent();

        ApiService apiService =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<ApiService>();

        BindingContext =
            new MyJobsViewModel(apiService);
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