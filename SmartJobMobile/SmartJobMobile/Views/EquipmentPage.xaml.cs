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

        var viewModel =
            new EquipmentViewModel(
                job,
                localDatabaseService,
                apiService,
                connectivityService);
        BindingContext = viewModel;

        SafetyChecklistHost.Children.Add(
            new ChecklistSectionView(job, ChecklistSection.SiteSafety));
        var equipmentChecklist = new ChecklistSectionView(job, ChecklistSection.EquipmentDetails);
        EquipmentChecklistHost.Children.Add(equipmentChecklist);
        viewModel.EquipmentRemoved += async (_, _) => await equipmentChecklist.RefreshAsync();
        WorkChecklistHost.Children.Add(
            new ChecklistSectionView(job, ChecklistSection.WorkCompleted));
    }

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}