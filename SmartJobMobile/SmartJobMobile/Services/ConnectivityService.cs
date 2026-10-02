namespace SmartJobMobile.Services;

public class ConnectivityService
{
    public bool IsOnline =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public string ConnectionStatus =>
        IsOnline ? "Online" : "Offline";

    public string ConnectionMessage =>
        IsOnline
            ? "Connected to the SmartJob service"
            : "No internet connection";

    public string ConnectionColor =>
        IsOnline
            ? "#4CAF50"
            : "#EF5350";

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged +=
            OnConnectivityChanged;
    }

    private void OnConnectivityChanged(
        object? sender,
        ConnectivityChangedEventArgs e)
    {
        bool isOnline =
            e.NetworkAccess == NetworkAccess.Internet;

        ConnectivityChanged?.Invoke(
            this,
            isOnline);
    }
}