using Microsoft.AspNetCore.Identity;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Identity;

/// <summary>
/// Hashes and verifies user passwords using ASP.NET Core's built-in PBKDF2-based hasher.
/// Never store or compare plain-text passwords anywhere else in the codebase.
/// </summary>
public class PasswordHashingService
{
    private readonly PasswordHasher<User> _hasher = new();

    /// <summary>Hashes a plain-text password for storage on User.PasswordHash.</summary>
    public string Hash(User user, string plainTextPassword) =>
        _hasher.HashPassword(user, plainTextPassword);

    /// <summary>Verifies a plain-text password against the user's stored hash.</summary>
    public bool Verify(User user, string plainTextPassword)
    {
        if (string.IsNullOrEmpty(user.PasswordHash)) return false;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, plainTextPassword);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}