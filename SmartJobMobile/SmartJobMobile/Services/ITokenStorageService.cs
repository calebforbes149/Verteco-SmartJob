namespace SmartJobMobile.Services;

public interface ITokenStorageService
{
    Task SaveAccessTokenAsync(string accessToken);

    Task<string?> GetAccessTokenAsync();

    Task SaveRefreshTokenAsync(string refreshToken);

    Task<string?> GetRefreshTokenAsync();

    Task SaveAccessTokenExpiryAsync(DateTimeOffset expiresAtUtc);

    Task<DateTimeOffset?> GetAccessTokenExpiryAsync();

    void ClearTokens();
}