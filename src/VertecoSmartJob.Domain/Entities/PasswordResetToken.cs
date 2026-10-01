using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

/// <summary>A single-use, time-limited token emailed to a user requesting a password reset.</summary>
public class PasswordResetToken : Entity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsUsed => UsedAtUtc is not null;
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
    public bool IsValid => !IsUsed && !IsExpired;

    public void MarkUsed() => UsedAtUtc = DateTime.UtcNow;
}