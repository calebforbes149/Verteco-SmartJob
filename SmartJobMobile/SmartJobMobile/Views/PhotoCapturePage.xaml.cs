using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class PhotoCapturePage : ContentPage
{
    private readonly PhotoCaptureViewModel _viewModel;

    public PhotoCapturePage(Job job)
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

        _viewModel =
            new PhotoCaptureViewModel(
                job,
                localDatabaseService,
                apiService,
                connectivityService);
        BindingContext = _viewModel;
    }

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void SaveEvidenceCaption_Clicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Guid id })
            await _viewModel.SaveCaptionAsync(id);
    }

    private async void RemoveSavedEvidence_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Guid id }) return;
        if (!await DisplayAlertAsync("Remove photo", "Remove this photo from the current job card?", "Remove", "Cancel")) return;
        await _viewModel.RemoveSavedEvidenceAsync(id);
    }

    private async void RemovePendingEvidence_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: int queueItemId }) return;
        if (!await DisplayAlertAsync("Remove queued photo", "Remove this queued photo? It will not be uploaded.", "Remove", "Cancel")) return;
        await _viewModel.RemovePendingEvidenceAsync(queueItemId);
    }
}