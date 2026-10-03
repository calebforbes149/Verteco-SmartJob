using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class JobDetailsPage : ContentPage
{
    public JobDetailsPage(Job job)
    {
        InitializeComponent();

        ApiService apiService =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<ApiService>();

        BindingContext =
            new JobDetailsViewModel(
                job,
                apiService);
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}