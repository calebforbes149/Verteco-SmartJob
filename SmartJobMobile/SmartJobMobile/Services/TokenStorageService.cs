using System.Globalization;

namespace SmartJobMobile.Services;

public class TokenStorageService : ITokenStorageService
{
    private const string AccessTokenKey = "smartjob_access_token";
    private const string RefreshTokenKey = "smartjob_refresh_token";
    private const string AccessTokenExpiryKey = "smartjob_access_token_expiry";

    public async Task SaveAccessTokenAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException(
                "Access token cannot be empty.",
                nameof(accessToken));
        }

        await SecureStorage.Default.SetAsync(
            AccessTokenKey,
            accessToken);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(
                AccessTokenKey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SecureStorage access token read failed: {ex.Message}");

            return null;
        }
    }

    public async Task SaveRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException(
                "Refresh token cannot be empty.",
                nameof(refreshToken));
        }

        await SecureStorage.Default.SetAsync(
            RefreshTokenKey,
            refreshToken);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(
                RefreshTokenKey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SecureStorage refresh token read failed: {ex.Message}");

            return null;
        }
    }

    public async Task SaveAccessTokenExpiryAsync(
        DateTimeOffset expiresAtUtc)
    {
        string expiryValue =
            expiresAtUtc
                .ToUniversalTime()
                .ToString(
                    "O",
                    CultureInfo.InvariantCulture);

        await SecureStorage.Default.SetAsync(
            AccessTokenExpiryKey,
            expiryValue);
    }

    public async Task<DateTimeOffset?> GetAccessTokenExpiryAsync()
    {
        try
        {
            string? expiryValue =
                await SecureStorage.Default.GetAsync(
                    AccessTokenExpiryKey);

            if (string.IsNullOrWhiteSpace(expiryValue))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(
                    expiryValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset expiresAtUtc))
            {
                return expiresAtUtc;
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SecureStorage expiry read failed: {ex.Message}");

            return null;
        }
    }

    public void ClearTokens()
    {
        SecureStorage.Default.Remove(
            AccessTokenKey);

        SecureStorage.Default.Remove(
            RefreshTokenKey);

        SecureStorage.Default.Remove(
            AccessTokenExpiryKey);
    }
}