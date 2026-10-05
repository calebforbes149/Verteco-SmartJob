using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

public class RefreshToken : Entity
{
    public Guid UserId { get; set; }

    // Only the SHA-256 hash of the refresh token is stored.
    // The raw token is only returned to the authenticated client.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsValid =>
        RevokedAtUtc is null &&
        ExpiresAtUtc > DateTime.UtcNow;

    public void Revoke(
        string? replacedByTokenHash = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}