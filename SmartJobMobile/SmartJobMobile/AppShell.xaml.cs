using SmartJobMobile.Services;
using SmartJobMobile.Views;

namespace SmartJobMobile;

public partial class AppShell : Shell
{
    private readonly SessionService
        _sessionService;

    public AppShell(
        SessionService sessionService)
    {
        InitializeComponent();

        _sessionService =
            sessionService;

        _sessionService.SessionExpired +=
            OnSessionExpired;

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

    private void OnSessionExpired(
        object? sender,
        EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(
            async () =>
            {
                await DisplayAlertAsync(
                    "Session Expired",
                    "Your SmartJob session has expired. Please sign in again.",
                    "OK");

                await GoToAsync(
                    "///LoginPage");
            });
    }
}