using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class OfflineSyncPage : ContentPage
{
    private readonly ConnectivityService
        _connectivityService;

    public OfflineSyncPage()
    {
        InitializeComponent();

        IServiceProvider services =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        _connectivityService =
            services.GetRequiredService<ConnectivityService>();

        LocalDatabaseService localDatabaseService =
            services.GetRequiredService<LocalDatabaseService>();

        ApiService apiService =
            services.GetRequiredService<ApiService>();

        BindingContext =
            new OfflineSyncViewModel(
                _connectivityService,
                localDatabaseService,
                apiService);
    }

    // =========================================================
    // DEVELOPMENT OFFLINE TEST
    // =========================================================

    private async void SimulateOfflineButton_Clicked(
        object? sender,
        EventArgs e)
    {
        _connectivityService
            .GoOfflineForTesting();

        await DisplayAlert(
            "Offline Test Mode",
            "SmartJob is now simulating an offline connection. Your VM is still connected to the internet.",
            "OK");
    }

    // =========================================================
    // RESTORE ONLINE
    // =========================================================

    private async void RestoreOnlineButton_Clicked(
        object? sender,
        EventArgs e)
    {
        _connectivityService
            .GoOnlineForTesting();

        await DisplayAlert(
            "Online Restored",
            "SmartJob is now using the real network connection again.",
            "OK");
    }

    // =========================================================
    // BACK
    // =========================================================

    private async void ClearOldTestDataButton_Clicked(
    object? sender,
    EventArgs e)
    {
        IServiceProvider services =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        LocalDatabaseService localDatabaseService =
            services.GetRequiredService<LocalDatabaseService>();

        bool confirm =
            await DisplayAlert(
                "Clear Old Test Data",
                "Remove the old unsupported test operations from the offline queue?",
                "CLEAR",
                "CANCEL");

        if (!confirm)
        {
            return;
        }

        int deleted =
            await localDatabaseService
                .ClearObsoleteTestOperationsAsync();

        await DisplayAlert(
            "Cleanup Complete",
            $"{deleted} old test operation(s) removed.",
            "OK");
    }

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}