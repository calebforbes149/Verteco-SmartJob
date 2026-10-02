using SmartJobMobile.Views;

namespace SmartJobMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(
            nameof(DashboardPage),
            typeof(DashboardPage));

        Routing.RegisterRoute(
            nameof(MyJobsPage),
            typeof(MyJobsPage));

        Routing.RegisterRoute(
            nameof(JobDetailsPage),
            typeof(JobDetailsPage));

        Routing.RegisterRoute(
            nameof(ChecklistPage),
            typeof(ChecklistPage));

        Routing.RegisterRoute(
            nameof(EquipmentPage),
            typeof(EquipmentPage));

        Routing.RegisterRoute(
            nameof(PhotoCapturePage),
            typeof(PhotoCapturePage));

        Routing.RegisterRoute(
            nameof(CommentsFlagPage),
            typeof(CommentsFlagPage));

        Routing.RegisterRoute(
            nameof(SignaturePage),
            typeof(SignaturePage));

        Routing.RegisterRoute(
            nameof(ReviewSubmitPage),
            typeof(ReviewSubmitPage));

        Routing.RegisterRoute(
            nameof(JobSubmittedPage),
            typeof(JobSubmittedPage));

        Routing.RegisterRoute(
                nameof(NotificationsPage),
                typeof(NotificationsPage));

        Routing.RegisterRoute(
                nameof(ProfilePage),
                typeof(ProfilePage));

        Routing.RegisterRoute(
            nameof(OfflineSyncPage),
            typeof(OfflineSyncPage));

    }
}