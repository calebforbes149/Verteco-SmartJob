using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartJobMobile.Models;

namespace SmartJobMobile.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorageService _tokenStorageService;

    public AuthenticationService(
        ITokenStorageService tokenStorageService)
    {
        _tokenStorageService = tokenStorageService;

        _httpClient = new HttpClient
        {
            BaseAddress =
                new Uri(ApiConfiguration.BaseUrl)
        };
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new MobileLoginRequest
            {
                Email = email.Trim(),
                Password = password
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    "api/v1/auth/login",
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AuthenticationResult.Failure(
                    await GetLoginErrorMessageAsync(
                        response,
                        cancellationToken));
            }

            MobileLoginResponse? loginResponse =
                await response.Content
                    .ReadFromJsonAsync<MobileLoginResponse>(
                        cancellationToken:
                            cancellationToken);

            if (loginResponse is null ||
                string.IsNullOrWhiteSpace(
                    loginResponse.AccessToken) ||
                string.IsNullOrWhiteSpace(
                    loginResponse.RefreshToken))
            {
                _tokenStorageService.ClearTokens();

                return AuthenticationResult.Failure(
                    "SmartJob did not return a valid authentication session.");
            }

            if (!string.Equals(
                    loginResponse.Role,
                    "Technician",
                    StringComparison.OrdinalIgnoreCase))
            {
                _tokenStorageService.ClearTokens();

                return AuthenticationResult.Failure(
                    "This mobile application is only available to technician accounts.");
            }

            await SaveSessionAsync(loginResponse);

            return AuthenticationResult.Success(
                loginResponse.FullName,
                loginResponse.Role);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob authentication connection error: {ex}");

            return AuthenticationResult.Failure(
                "Unable to connect to SmartJob. Check your connection and try again.");
        }
        catch (TaskCanceledException)
        {
            return AuthenticationResult.Failure(
                "The SmartJob login request timed out. Please try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob authentication error: {ex}");

            return AuthenticationResult.Failure(
                "An unexpected error occurred while signing in.");
        }
    }

    private async Task SaveSessionAsync(
        MobileLoginResponse loginResponse)
    {
        await _tokenStorageService
            .SaveAccessTokenAsync(
                loginResponse.AccessToken);

        await _tokenStorageService
            .SaveRefreshTokenAsync(
                loginResponse.RefreshToken);

        await _tokenStorageService
            .SaveAccessTokenExpiryAsync(
                loginResponse
                    .AccessTokenExpiresAtUtc
                    .ToUniversalTime());
    }

    private static async Task<string>
        GetLoginErrorMessageAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        string? serverMessage =
            await TryReadProblemTitleAsync(
                response,
                cancellationToken);

        if (!string.IsNullOrWhiteSpace(
                serverMessage))
        {
            return serverMessage;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                "Invalid email or password.",

            HttpStatusCode.Forbidden =>
                "Your account is not currently allowed to sign in.",

            HttpStatusCode.TooManyRequests =>
                "Too many login attempts. Please wait before trying again.",

            _ when (int)response.StatusCode == 423 =>
                "Your account is temporarily locked due to repeated failed login attempts.",

            _ =>
                "SmartJob could not sign you in. Please try again."
        };
    }

    private static async Task<string?>
        TryReadProblemTitleAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        try
        {
            string content =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            if (string.IsNullOrWhiteSpace(
                    content))
            {
                return null;
            }

            using JsonDocument document =
                JsonDocument.Parse(content);

            if (document.RootElement
                .TryGetProperty(
                    "title",
                    out JsonElement titleElement))
            {
                return titleElement.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}