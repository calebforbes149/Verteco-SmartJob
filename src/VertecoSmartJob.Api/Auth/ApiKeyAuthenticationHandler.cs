using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using VertecoSmartJob.Application.Abstractions;

namespace VertecoSmartJob.Api.Auth;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IUserRepository users)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "SmartJobKey";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-SmartJob-Key", out var values) || values.Count != 1 ||
            string.IsNullOrWhiteSpace(values[0])) return AuthenticateResult.NoResult();

        var candidate = SHA256.HashData(Encoding.UTF8.GetBytes(values[0]!));
        foreach (var account in configuration.GetSection("ApiKeys:Accounts").GetChildren())
        {
            var key = account["Key"];
            var email = account["Email"];
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(email)) continue;
            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            if (!CryptographicOperations.FixedTimeEquals(candidate, expected)) continue;

            var user = await users.GetByEmailAsync(email, Context.RequestAborted);
            if (user is null || user.IsSuspended) return AuthenticateResult.Fail("Account unavailable.");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };
            if (user.TechnicianId is { } technicianId)
                claims.Add(new Claim("TechnicianId", technicianId.ToString()));
            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName));
        }

        return AuthenticateResult.Fail("Invalid API key.");
    }
}
