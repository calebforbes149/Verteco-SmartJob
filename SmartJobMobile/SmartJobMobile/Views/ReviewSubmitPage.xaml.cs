using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class ReviewSubmitPage : ContentPage
{
    public ReviewSubmitPage()
    {
        InitializeComponent();

        LocalDatabaseService localDatabaseService =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<LocalDatabaseService>();

        BindingContext =
            new ReviewSubmitViewModel(
                localDatabaseService);
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}