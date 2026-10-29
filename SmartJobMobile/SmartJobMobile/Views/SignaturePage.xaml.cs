using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class SignaturePage : ContentPage
{
    private readonly SignatureViewModel _viewModel;

    public SignaturePage(Job job)
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

        _viewModel = new SignatureViewModel(
            job,
            localDatabaseService,
            apiService,
            connectivityService);

        BindingContext = _viewModel;

        // Once a signature upload is confirmed, clear the pad that was just
        // submitted so a second tap on Continue can't re-upload the same
        // drawing as a duplicate signature.
        _viewModel.SignaturesUploaded += () =>
        {
            TechnicianSignaturePad.Clear();
            ClientSignaturePad.Clear();
        };
    }

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ClearTechnicianSignature_Clicked(
        object? sender,
        EventArgs e)
    {
        TechnicianSignaturePad.Clear();
    }

    private void ClearClientSignature_Clicked(
        object? sender,
        EventArgs e)
    {
        ClientSignaturePad.Clear();
    }

    private async void ContinueToReview_Clicked(
        object? sender,
        EventArgs e)
    {
        try
        {
            if (!_viewModel.CanContinue)
            {
                // CanContinue being false here means a tap silently did
                // nothing before - this is the "sometimes it just does
                // nothing" glitch. Tell the user what's actually going on
                // instead of staying quiet.
                string reason = _viewModel.IsSaving
                    ? "Still saving the previous signatures. Please wait a moment and try again."
                    : _viewModel.IsLoadingExisting
                        ? "Still loading this job's saved signatures. Please wait a moment and try again."
                        : "This job's saved signatures haven't finished loading (often a connection issue). Please check your connection and reopen this page.";

                await DisplayAlertAsync(
                    "Please wait",
                    reason,
                    "OK");

                return;
            }

            if (TechnicianSignaturePad.Lines.Count == 0 && !_viewModel.HasTechnicianSignature)
            {
                await DisplayAlertAsync(
                    "Technician Signature Required",
                    "Please ask the technician to sign before continuing.",
                    "OK");

                return;
            }

            if (ClientSignaturePad.Lines.Count == 0 && !_viewModel.HasClientSignature)
            {
                await DisplayAlertAsync(
                    "Client Signature Required",
                    "Please ask the client to sign before continuing.",
                    "OK");

                return;
            }

            string? technicianSignaturePath = null;
            if (TechnicianSignaturePad.Lines.Count > 0)
            {
                technicianSignaturePath = Path.Combine(FileSystem.CacheDirectory,
                    $"technician_signature_{Guid.NewGuid():N}.png");
                using Stream technicianStream = await TechnicianSignaturePad.GetImageStream(500, 200);
                using FileStream fileStream = File.Create(technicianSignaturePath);
                await technicianStream.CopyToAsync(fileStream);
            }

            string? clientSignaturePath = null;
            if (ClientSignaturePad.Lines.Count > 0)
            {
                clientSignaturePath = Path.Combine(FileSystem.CacheDirectory,
                    $"client_signature_{Guid.NewGuid():N}.png");
                using Stream clientStream = await ClientSignaturePad.GetImageStream(500, 200);
                using FileStream fileStream = File.Create(clientSignaturePath);
                await clientStream.CopyToAsync(fileStream);
            }

            await _viewModel.SubmitDrawnSignaturesAsync(
                technicianSignaturePath,
                clientSignaturePath);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Signature Error",
                $"The signatures could not be processed.\n\n{ex.Message}",
                "OK");
        }
    }

    private async void RemoveTechnicianSignature_Clicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Remove signature", "Remove the saved technician signature from this job card?", "Remove", "Cancel")) return;
        await _viewModel.RemoveExistingSignatureAsync("Technician");
    }

    private async void RemoveClientSignature_Clicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Remove signature", "Remove the saved client signature from this job card?", "Remove", "Cancel")) return;
        await _viewModel.RemoveExistingSignatureAsync("Client");
    }

    private void ReplaceTechnicianSignature_Clicked(object? sender, EventArgs e)
    {
        // Make sure the pad is empty before it's shown again, so the user
        // draws a fresh signature rather than resubmitting an old one.
        TechnicianSignaturePad.Clear();
        _viewModel.BeginReplaceTechnicianSignature();
    }

    private void ReplaceClientSignature_Clicked(object? sender, EventArgs e)
    {
        ClientSignaturePad.Clear();
        _viewModel.BeginReplaceClientSignature();
    }
}