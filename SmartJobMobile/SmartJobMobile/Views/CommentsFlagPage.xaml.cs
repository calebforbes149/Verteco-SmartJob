using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class CommentsFlagPage : ContentPage
{
    public CommentsFlagPage(Job job)
    {
        InitializeComponent();

        IServiceProvider services =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        ApiService apiService =
            services.GetRequiredService<ApiService>();

        ConnectivityService connectivityService =
            services.GetRequiredService<ConnectivityService>();

        BindingContext =
            new CommentsFlagViewModel(
                job,
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