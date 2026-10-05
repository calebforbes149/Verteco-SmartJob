namespace SmartJobMobile.Services;

public sealed class SessionService
{
    private int _sessionExpiredRaised;

    public event EventHandler? SessionExpired;

    public void MarkSessionActive()
    {
        Interlocked.Exchange(
            ref _sessionExpiredRaised,
            0);
    }

    public void NotifySessionExpired()
    {
        if (Interlocked.CompareExchange(
                ref _sessionExpiredRaised,
                1,
                0) != 0)
        {
            return;
        }

        SessionExpired?.Invoke(
            this,
            EventArgs.Empty);
    }
}