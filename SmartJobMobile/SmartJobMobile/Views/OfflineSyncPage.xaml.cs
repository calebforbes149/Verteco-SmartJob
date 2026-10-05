using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class OfflineSyncPage : ContentPage
{
    public OfflineSyncPage()
    {
        InitializeComponent();

        IServiceProvider services =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        ConnectivityService connectivityService =
            services.GetRequiredService<ConnectivityService>();

        LocalDatabaseService localDatabaseService =
            services.GetRequiredService<LocalDatabaseService>();

        ApiService apiService =
            services.GetRequiredService<ApiService>();

        BindingContext =
            new OfflineSyncViewModel(
                connectivityService,
                localDatabaseService,
                apiService);
    }

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}