using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Auth;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Infrastructure.Identity;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IUserRepository users,
    IPasswordResetTokenRepository resetTokens,
    IRefreshTokenRepository refreshTokens,
    IAuditLogRepository audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    PasswordHashingService hasher,
    JwtTokenService tokens,
    IEmailSender emailSender,
    IConfiguration configuration)
    : ControllerBase
{
    private const int MaxFailedAttempts = 5;

    private static readonly TimeSpan LockoutDuration =
        TimeSpan.FromMinutes(15);

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken ct)
    {
        var user =
            await users.GetByEmailAsync(
                request.Email,
                ct);

        // Deliberately identical response whether the
        // email doesn't exist or the password is wrong.
        if (user is null || user.IsSuspended)
        {
            if (user is not null)
            {
                await LogAuthEventAsync(
                    user.Id,
                    "LoginFailed",
                    $"Email: {request.Email}",
                    ct);

                await unitOfWork.SaveChangesAsync(
                    ct);
            }

            return Unauthorized(
                new ProblemDetails
                {
                    Title =
                        "Invalid email or password."
                });
        }

        if (user.IsCurrentlyLockedOut())
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                new ProblemDetails
                {
                    Title =
                        "Account temporarily locked due to repeated failed attempts. Try again later."
                });
        }

        if (!hasher.Verify(
                user,
                request.Password))
        {
            user.RegisterFailedLogin(
                MaxFailedAttempts,
                LockoutDuration);

            var action =
                user.IsCurrentlyLockedOut()
                    ? "AccountLockedOut"
                    : "LoginFailed";

            await LogAuthEventAsync(
                user.Id,
                action,
                $"Failed attempt {user.FailedLoginAttempts}/{MaxFailedAttempts}",
                ct);

            await unitOfWork.SaveChangesAsync(
                ct);

            return Unauthorized(
                new ProblemDetails
                {
                    Title =
                        "Invalid email or password."
                });
        }

        if (!user.IsEmailConfirmed)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Title =
                        "Confirm your email before signing in."
                });
        }

        user.ResetFailedLogins();

        string accessToken =
            tokens.IssueAccessToken(
                user,
                out DateTime accessTokenExpiresAtUtc);

        string rawRefreshToken =
            tokens.GenerateRefreshToken();

        string refreshTokenHash =
            tokens.HashRefreshToken(
                rawRefreshToken);

        await refreshTokens.AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash =
                    refreshTokenHash,
                ExpiresAtUtc =
                    tokens.GetRefreshTokenExpiryUtc()
            },
            ct);

        await LogAuthEventAsync(
            user.Id,
            "LoginSucceeded",
            user.Email,
            ct);

        await unitOfWork.SaveChangesAsync(
            ct);

        return Ok(
            CreateLoginResponse(
                user,
                accessToken,
                rawRefreshToken,
                accessTokenExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(
                request.RefreshToken))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title =
                        "Invalid or expired refresh token."
                });
        }

        string suppliedTokenHash =
            tokens.HashRefreshToken(
                request.RefreshToken);

        RefreshToken? storedToken =
            await refreshTokens
                .GetByTokenHashAsync(
                    suppliedTokenHash,
                    ct);

        if (storedToken is null ||
            !storedToken.IsValid)
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title =
                        "Invalid or expired refresh token."
                });
        }

        User? user =
            await users.GetByIdAsync(
                storedToken.UserId,
                ct);

        if (user is null ||
            user.IsSuspended ||
            !user.IsEmailConfirmed)
        {
            storedToken.Revoke();

            await unitOfWork.SaveChangesAsync(
                ct);

            return Unauthorized(
                new ProblemDetails
                {
                    Title =
                        "This session is no longer valid."
                });
        }

        string newRawRefreshToken =
            tokens.GenerateRefreshToken();

        string newRefreshTokenHash =
            tokens.HashRefreshToken(
                newRawRefreshToken);

        storedToken.Revoke(
            newRefreshTokenHash);

        await refreshTokens.AddAsync(
            new RefreshToken
            {
                UserId =
                    user.Id,

                TokenHash =
                    newRefreshTokenHash,

                ExpiresAtUtc =
                    tokens.GetRefreshTokenExpiryUtc()
            },
            ct);

        string accessToken =
            tokens.IssueAccessToken(
                user,
                out DateTime accessTokenExpiresAtUtc);

        await LogAuthEventAsync(
            user.Id,
            "RefreshTokenRotated",
            "A new access token and refresh token were issued.",
            ct);

        await unitOfWork.SaveChangesAsync(
            ct);

        return Ok(
            CreateLoginResponse(
                user,
                accessToken,
                newRawRefreshToken,
                accessTokenExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(
                request.RefreshToken))
        {
            return NoContent();
        }

        string suppliedTokenHash =
            tokens.HashRefreshToken(
                request.RefreshToken);

        RefreshToken? storedToken =
            await refreshTokens
                .GetByTokenHashAsync(
                    suppliedTokenHash,
                    ct);

        // Logout deliberately returns the same response
        // whether or not the token exists.
        if (storedToken is null)
        {
            return NoContent();
        }

        if (storedToken.RevokedAtUtc is null)
        {
            storedToken.Revoke();

            await LogAuthEventAsync(
                storedToken.UserId,
                "Logout",
                "Refresh token revoked.",
                ct);

            await unitOfWork.SaveChangesAsync(
                ct);
        }

        return NoContent();
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("users/{id:guid}/set-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SetPassword(
        Guid id,
        SetPasswordRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(
                request.NewPassword) ||
            request.NewPassword.Length < 8)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title =
                        "Password must be at least 8 characters."
                });
        }

        var user =
            await users.GetByIdAsync(
                id,
                ct);

        if (user is null)
        {
            return NotFound();
        }

        var hash =
            hasher.Hash(
                user,
                request.NewPassword);

        user.SetPasswordHash(
            hash);

        // A Manager-set password implies the
        // account is now usable.
        user.ConfirmEmail();

        // Clear any prior lockout state.
        user.ResetFailedLogins();

        await LogAuthEventAsync(
            currentUser.UserId,
            "PasswordSetByManager",
            $"Target user: {user.Email}",
            ct);

        await unitOfWork.SaveChangesAsync(
            ct);

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken ct)
    {
        // Return the same result for every address when mail delivery is unavailable.
        // Otherwise the UI would claim that a link was sent when SMTP is disabled.
        if (string.IsNullOrWhiteSpace(configuration["Smtp:Host"]) ||
            configuration.GetValue<int?>("Smtp:Port") is not > 0 ||
            string.IsNullOrWhiteSpace(configuration["Smtp:Username"]) ||
            string.IsNullOrWhiteSpace(configuration["Smtp:Password"]))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails { Title = "Password reset email is temporarily unavailable." });
        }

        var user =
            await users.GetByEmailAsync(
                request.Email,
                ct);

        // Always return the same response whether or not
        // the email exists.
        if (user is not null &&
            !user.IsSuspended)
        {
            var rawToken =
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(
                        48));

            var tokenHash =
                Convert.ToBase64String(
                    SHA256.HashData(
                        System.Text.Encoding.UTF8
                            .GetBytes(
                                rawToken)));

            await resetTokens.AddAsync(
                new PasswordResetToken
                {
                    UserId =
                        user.Id,

                    TokenHash =
                        tokenHash,

                    ExpiresAtUtc =
                        DateTime.UtcNow
                            .AddHours(1)
                },
                ct);

            await LogAuthEventAsync(
                user.Id,
                "PasswordResetRequested",
                user.Email,
                ct);

            await unitOfWork.SaveChangesAsync(
                ct);

            var webBaseUrl =
                configuration[
                    "WebApp:BaseUrl"]
                ?? "https://localhost:7088";

            var resetLink =
                $"{webBaseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(rawToken)}";

            await emailSender.SendAsync(
                user.Email,
                "Reset your SmartJob password",
                $"<p>Hi {user.FullName},</p><p>Click the link below to reset your SmartJob password. This link expires in 1 hour.</p><p><a href=\"{resetLink}\">{resetLink}</a></p><p>If you didn't request this, you can safely ignore this email.</p>",
                ct);
        }

        return Ok(
            new
            {
                message =
                    "If that email is registered, a reset link has been sent."
            });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken ct)
    {
        var tokenHash =
            Convert.ToBase64String(
                SHA256.HashData(
                    System.Text.Encoding.UTF8
                        .GetBytes(
                            request.Token)));

        var resetToken =
            await resetTokens
                .GetByTokenHashAsync(
                    tokenHash,
                    ct);

        if (resetToken is null ||
            !resetToken.IsValid)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title =
                        "This reset link is invalid or has expired."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.NewPassword) ||
            request.NewPassword.Length < 8)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title =
                        "Password must be at least 8 characters."
                });
        }

        var user =
            await users.GetByIdAsync(
                resetToken.UserId,
                ct);

        if (user is null)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title =
                        "This reset link is invalid or has expired."
                });
        }

        user.SetPasswordHash(
            hasher.Hash(
                user,
                request.NewPassword));

        user.ConfirmEmail();
        user.ResetFailedLogins();

        resetToken.MarkUsed();

        await LogAuthEventAsync(
            user.Id,
            "PasswordReset",
            user.Email,
            ct);

        await unitOfWork.SaveChangesAsync(
            ct);

        return NoContent();
    }

    private static LoginResponse CreateLoginResponse(
        User user,
        string accessToken,
        string refreshToken,
        DateTime accessTokenExpiresAtUtc)
    {
        return new LoginResponse
        {
            AccessToken =
                accessToken,

            RefreshToken =
                refreshToken,

            AccessTokenExpiresAtUtc =
                accessTokenExpiresAtUtc,

            FullName =
                user.FullName,

            Role =
                user.Role
        };
    }

    private async Task LogAuthEventAsync(
        Guid? actorUserId,
        string action,
        string details,
        CancellationToken ct)
    {
        await audit.AddAsync(
            new AuditLog
            {
                ActorUserId =
                    actorUserId,

                Action =
                    action,

                Details =
                    details
            },
            ct);
    }
}
