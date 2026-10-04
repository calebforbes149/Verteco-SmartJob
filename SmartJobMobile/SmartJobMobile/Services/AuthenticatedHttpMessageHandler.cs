using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SmartJobMobile.Models;

namespace SmartJobMobile.Services;

public sealed class AuthenticatedHttpMessageHandler
    : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock =
        new(1, 1);

    private static readonly TimeSpan RefreshBeforeExpiry =
        TimeSpan.FromMinutes(1);

    private readonly ITokenStorageService
        _tokenStorageService;

    public AuthenticatedHttpMessageHandler(
        ITokenStorageService tokenStorageService)
    {
        _tokenStorageService = tokenStorageService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? accessToken =
            await GetValidAccessTokenAsync(
                cancellationToken);

        HttpRequestMessage? retryRequest = null;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            retryRequest =
                await CloneRequestAsync(
                    request,
                    cancellationToken);
        }

        HttpResponseMessage response =
            await base.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode !=
                HttpStatusCode.Unauthorized ||
            retryRequest is null)
        {
            return response;
        }

        string? refreshedAccessToken =
            await RefreshAccessTokenAsync(
                forceRefresh: true,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(
                refreshedAccessToken))
        {
            return response;
        }

        response.Dispose();

        retryRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                refreshedAccessToken);

        return await base.SendAsync(
            retryRequest,
            cancellationToken);
    }

    private async Task<string?>
        GetValidAccessTokenAsync(
            CancellationToken cancellationToken)
    {
        string? accessToken =
            await _tokenStorageService
                .GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(
                accessToken))
        {
            return null;
        }

        DateTimeOffset? expiresAtUtc =
            await _tokenStorageService
                .GetAccessTokenExpiryAsync();

        if (!expiresAtUtc.HasValue)
        {
            return accessToken;
        }

        bool shouldRefresh =
            expiresAtUtc.Value <=
            DateTimeOffset.UtcNow
                .Add(RefreshBeforeExpiry);

        if (!shouldRefresh)
        {
            return accessToken;
        }

        return await RefreshAccessTokenAsync(
            forceRefresh: false,
            cancellationToken);
    }

    private async Task<string?>
        RefreshAccessTokenAsync(
            bool forceRefresh,
            CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(
            cancellationToken);

        try
        {
            string? currentAccessToken =
                await _tokenStorageService
                    .GetAccessTokenAsync();

            DateTimeOffset? currentExpiry =
                await _tokenStorageService
                    .GetAccessTokenExpiryAsync();

            // Another request may already have refreshed
            // the token while this request waited for
            // the semaphore.
            if (!forceRefresh &&
                !string.IsNullOrWhiteSpace(
                    currentAccessToken) &&
                currentExpiry.HasValue &&
                currentExpiry.Value >
                    DateTimeOffset.UtcNow
                        .Add(RefreshBeforeExpiry))
            {
                return currentAccessToken;
            }

            string? refreshToken =
                await _tokenStorageService
                    .GetRefreshTokenAsync();

            if (string.IsNullOrWhiteSpace(
                    refreshToken))
            {
                _tokenStorageService.ClearTokens();
                return null;
            }

            using var refreshClient =
                new HttpClient
                {
                    BaseAddress =
                        new Uri(
                            ApiConfiguration.BaseUrl)
                };

            var refreshRequest =
                new MobileRefreshTokenRequest
                {
                    RefreshToken =
                        refreshToken
                };

            using HttpResponseMessage response =
                await refreshClient.PostAsJsonAsync(
                    "api/v1/auth/refresh",
                    refreshRequest,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is
                    HttpStatusCode.Unauthorized or
                    HttpStatusCode.Forbidden)
                {
                    // Refresh token has expired, was
                    // revoked or the account is no
                    // longer allowed to authenticate.
                    _tokenStorageService.ClearTokens();
                }

                return null;
            }

            MobileLoginResponse? refreshResponse =
                await response.Content
                    .ReadFromJsonAsync<MobileLoginResponse>(
                        cancellationToken:
                            cancellationToken);

            if (refreshResponse is null ||
                string.IsNullOrWhiteSpace(
                    refreshResponse.AccessToken) ||
                string.IsNullOrWhiteSpace(
                    refreshResponse.RefreshToken))
            {
                _tokenStorageService.ClearTokens();
                return null;
            }

            if (!string.Equals(
                    refreshResponse.Role,
                    "Technician",
                    StringComparison.OrdinalIgnoreCase))
            {
                _tokenStorageService.ClearTokens();
                return null;
            }

            await _tokenStorageService
                .SaveAccessTokenAsync(
                    refreshResponse.AccessToken);

            await _tokenStorageService
                .SaveRefreshTokenAsync(
                    refreshResponse.RefreshToken);

            await _tokenStorageService
                .SaveAccessTokenExpiryAsync(
                    refreshResponse
                        .AccessTokenExpiresAtUtc
                        .ToUniversalTime());

            return refreshResponse.AccessToken;
        }
        catch (HttpRequestException ex)
        {
            // A network outage does not mean the user's
            // session should immediately be deleted.
            System.Diagnostics.Debug.WriteLine(
                $"Token refresh connection error: {ex}");

            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Token refresh error: {ex}");

            return null;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage>
        CloneRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
    {
        var clone =
            new HttpRequestMessage(
                request.Method,
                request.RequestUri)
            {
                Version =
                    request.Version,

                VersionPolicy =
                    request.VersionPolicy
            };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }

        if (request.Content is not null)
        {
            byte[] contentBytes =
                await request.Content
                    .ReadAsByteArrayAsync(
                        cancellationToken);

            clone.Content =
                new ByteArrayContent(
                    contentBytes);

            foreach (
                var header
                in request.Content.Headers)
            {
                clone.Content.Headers
                    .TryAddWithoutValidation(
                        header.Key,
                        header.Value);
            }
        }

        return clone;
    }
}