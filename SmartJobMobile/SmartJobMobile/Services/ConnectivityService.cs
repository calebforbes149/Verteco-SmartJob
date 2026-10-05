namespace SmartJobMobile.Services;

public class ConnectivityService
{
    // =========================================================
    // DEVELOPMENT OFFLINE SIMULATION
    // =========================================================

    private bool _simulateOffline;

    public bool IsSimulatingOffline =>
        _simulateOffline;

    // =========================================================
    // CONNECTION STATE
    // =========================================================

    public bool IsOnline =>
        !_simulateOffline &&
        Connectivity.Current.NetworkAccess ==
            NetworkAccess.Internet;

    public string ConnectionStatus =>
        IsOnline
            ? "Online"
            : "Offline";

    public string ConnectionMessage =>
        _simulateOffline
            ? "Offline mode is being simulated"
            : IsOnline
                ? "Connected to the SmartJob service"
                : "No internet connection";

    public string ConnectionColor =>
        IsOnline
            ? "#4CAF50"
            : "#EF5350";

    // =========================================================
    // EVENT
    // =========================================================

    public event EventHandler<bool>?
        ConnectivityChanged;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged +=
            OnConnectivityChanged;
    }

    // =========================================================
    // REAL DEVICE CONNECTIVITY CHANGED
    // =========================================================

    private void OnConnectivityChanged(
        object? sender,
        ConnectivityChangedEventArgs e)
    {
        bool isOnline =
            !_simulateOffline &&
            e.NetworkAccess ==
                NetworkAccess.Internet;

        ConnectivityChanged?.Invoke(
            this,
            isOnline);
    }

    // =========================================================
    // DEVELOPMENT TESTING
    // =========================================================

    public void SetSimulatedOffline(
        bool simulateOffline)
    {
        _simulateOffline =
            simulateOffline;

        ConnectivityChanged?.Invoke(
            this,
            IsOnline);
    }

    public void GoOfflineForTesting()
    {
        SetSimulatedOffline(true);
    }

    public void GoOnlineForTesting()
    {
        SetSimulatedOffline(false);
    }
}