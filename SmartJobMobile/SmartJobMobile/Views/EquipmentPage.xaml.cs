using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class EquipmentPage : ContentPage
{
    public EquipmentPage(Job job)
    {
        InitializeComponent();

        IServiceProvider services =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        LocalDatabaseService localDatabaseService =
            services.GetRequiredService<LocalDatabaseService>();

        ApiService apiService =
            services.GetRequiredService<ApiService>();

        ConnectivityService connectivityService =
            services.GetRequiredService<ConnectivityService>();

        BindingContext =
            new EquipmentViewModel(
                job,
                localDatabaseService,
                apiService,
                connectivityService);
    }

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}