using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class ChecklistPage : ContentPage
{
    public ChecklistPage(Job job)
    {
        InitializeComponent();

        LocalDatabaseService localDatabaseService =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<LocalDatabaseService>();

        ApiService apiService =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<ApiService>();

        BindingContext =
            new ChecklistViewModel(
                job,
                localDatabaseService,
                apiService);
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}