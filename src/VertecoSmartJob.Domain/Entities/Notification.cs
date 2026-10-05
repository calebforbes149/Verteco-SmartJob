using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

/// <summary>An in-app notification for a specific user, created alongside the email
/// sent by EmailNotificationService for the same event.</summary>
public class Notification : Entity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? JobCardId { get; set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public void MarkRead() => IsRead = true;
}