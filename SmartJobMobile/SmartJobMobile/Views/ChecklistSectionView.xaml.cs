using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public enum ChecklistSection
{
    SiteSafety,
    EquipmentDetails,
    WorkCompleted,
    ResultsAndIssues
}

public partial class ChecklistSectionView : ContentView
{
    private readonly ChecklistViewModel _viewModel;
    private static readonly HashSet<string> EquipmentChecks = new(StringComparer.OrdinalIgnoreCase)
    {
        "Confirm site access and safety controls",
        "Record equipment and serial numbers",
        "Complete installation, inspection or maintenance work"
    };

    private static readonly HashSet<string> AutomaticChecks = new(StringComparer.OrdinalIgnoreCase)
    {
        "Capture photographic evidence",
        "Obtain technician and client sign-off"
    };

    public ChecklistSectionView(Job job, ChecklistSection section)
    {
        InitializeComponent();

        SectionTitle.Text = section switch
        {
            ChecklistSection.SiteSafety => "SITE ACCESS & SAFETY",
            ChecklistSection.EquipmentDetails => "CONFIRM EQUIPMENT DETAILS",
            ChecklistSection.WorkCompleted => "CONFIRM WORK COMPLETED",
            _ => "CONFIRM RESULTS RECORDED"
        };
        SectionHint.Text = "Tick each check after you have done it. You can leave a check unticked and return to it before submitting.";

        Func<string, bool> visibleItem = section switch
        {
            ChecklistSection.SiteSafety => text => text.Equals(
                "Confirm site access and safety controls", StringComparison.OrdinalIgnoreCase),
            ChecklistSection.EquipmentDetails => text => text.Equals(
                "Record equipment and serial numbers", StringComparison.OrdinalIgnoreCase),
            ChecklistSection.WorkCompleted => text => text.Equals(
                "Complete installation, inspection or maintenance work", StringComparison.OrdinalIgnoreCase),
            _ => text => !EquipmentChecks.Contains(text) && !AutomaticChecks.Contains(text)
        };

        IServiceProvider services = Application.Current!.Handler!.MauiContext!.Services;
        _viewModel = new ChecklistViewModel(
            job,
            services.GetRequiredService<LocalDatabaseService>(),
            services.GetRequiredService<ApiService>(),
            services.GetRequiredService<ConnectivityService>(),
            visibleItem);
        BindingContext = _viewModel;
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();
}
