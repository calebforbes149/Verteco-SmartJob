using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Identity;

public class JwtTokenService(
    IConfiguration configuration)
{
    private const int DefaultAccessTokenMinutes = 30;
    private const int DefaultRefreshTokenDays = 7;

    public string IssueAccessToken(
        User user)
    {
        return IssueAccessToken(
            user,
            out _);
    }

    public string IssueAccessToken(
        User user,
        out DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                user.FullName),

            new(
                ClaimTypes.Email,
                user.Email),

            new(
                ClaimTypes.Role,
                user.Role)
        };

        if (user.TechnicianId is { } technicianId)
        {
            claims.Add(
                new Claim(
                    "TechnicianId",
                    technicianId.ToString()));
        }

        var signingKey =
            configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "Jwt:SigningKey is not configured.");

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    signingKey));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        int accessTokenMinutes =
            configuration.GetValue<int?>(
                "Jwt:AccessTokenMinutes")
            ?? DefaultAccessTokenMinutes;

        expiresAtUtc =
            DateTime.UtcNow.AddMinutes(
                accessTokenMinutes);

        var token =
            new JwtSecurityToken(
                issuer:
                    configuration["Jwt:Issuer"],

                audience:
                    configuration["Jwt:Audience"],

                claims:
                    claims,

                expires:
                    expiresAtUtc,

                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        byte[] randomBytes =
            RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(
            randomBytes);
    }

    public string HashRefreshToken(
        string refreshToken)
    {
        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(
                refreshToken);

        byte[] hash =
            SHA256.HashData(
                tokenBytes);

        return Convert.ToBase64String(
            hash);
    }

    public DateTime GetRefreshTokenExpiryUtc()
    {
        int refreshTokenDays =
            configuration.GetValue<int?>(
                "Jwt:RefreshTokenDays")
            ?? DefaultRefreshTokenDays;

        return DateTime.UtcNow.AddDays(
            refreshTokenDays);
    }
}