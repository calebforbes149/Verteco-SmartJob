using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class PhotoCapturePage : ContentPage
{
    public PhotoCapturePage()
    {
        InitializeComponent();

        LocalDatabaseService localDatabaseService =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<LocalDatabaseService>();

        BindingContext =
            new PhotoCaptureViewModel(
                localDatabaseService);
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}