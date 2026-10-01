using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Auth;
using VertecoSmartJob.Infrastructure.Identity;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    PasswordHashingService hasher,
    JwtTokenService tokens) : ControllerBase
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email, ct);

        // Deliberately identical response whether the email doesn't exist or the password is wrong —
        // telling an attacker "that email isn't registered" is itself useful information to withhold.
        if (user is null || user.IsSuspended)
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });

        if (user.IsCurrentlyLockedOut())
            return StatusCode(StatusCodes.Status423Locked,
                new ProblemDetails { Title = "Account temporarily locked due to repeated failed attempts. Try again later." });

        if (!hasher.Verify(user, request.Password))
        {
            user.RegisterFailedLogin(MaxFailedAttempts, LockoutDuration);
            await unitOfWork.SaveChangesAsync(ct);
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        if (!user.IsEmailConfirmed)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ProblemDetails { Title = "Confirm your email before signing in." });

        user.ResetFailedLogins();
        await unitOfWork.SaveChangesAsync(ct);

        var accessToken = tokens.IssueAccessToken(user);
        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            FullName = user.FullName,
            Role = user.Role
        });
    }
}