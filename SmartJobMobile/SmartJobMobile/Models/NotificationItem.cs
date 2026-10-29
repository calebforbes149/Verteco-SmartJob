using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartJobMobile.Models;

public class NotificationItem : INotifyPropertyChanged
{
    private bool _isRead;

    public Guid ApiId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    // A plain auto-property here meant marking a notification as
    // read never told the UI anything changed - the list item kept
    // looking "unread" forever, even after it had been tapped and
    // the server confirmed it. This needs to raise PropertyChanged
    // so the page can actually show read vs unread.
    public bool IsRead
    {
        get => _isRead;
        set
        {
            if (_isRead == value) return;
            _isRead = value;
            OnPropertyChanged();
        }
    }

    public DateTime CreatedAtUtc { get; set; }

    public string TimeAgo => FormatTimeAgo(CreatedAtUtc);

    private static string FormatTimeAgo(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
