using System.Net;
using System.Net.Http.Json;
using System.Text;
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
            BaseAddress = new Uri(
                "http://localhost:5196/")
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
                        cancellationToken: cancellationToken);

            if (loginResponse is null ||
                string.IsNullOrWhiteSpace(
                    loginResponse.AccessToken))
            {
                return AuthenticationResult.Failure(
                    "SmartJob did not return a valid authentication token.");
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

            await _tokenStorageService.SaveAccessTokenAsync(
                loginResponse.AccessToken);

            DateTimeOffset? expiry =
                TryReadTokenExpiry(
                    loginResponse.AccessToken);

            if (expiry.HasValue)
            {
                await _tokenStorageService
                    .SaveAccessTokenExpiryAsync(
                        expiry.Value);
            }

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

    private static async Task<string>
        GetLoginErrorMessageAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        string? serverMessage =
            await TryReadProblemTitleAsync(
                response,
                cancellationToken);

        if (!string.IsNullOrWhiteSpace(serverMessage))
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

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using JsonDocument document =
                JsonDocument.Parse(content);

            if (document.RootElement.TryGetProperty(
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

    private static DateTimeOffset?
        TryReadTokenExpiry(
            string accessToken)
    {
        try
        {
            string[] tokenParts =
                accessToken.Split('.');

            if (tokenParts.Length < 2)
            {
                return null;
            }

            string payload =
                tokenParts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');

            int remainder =
                payload.Length % 4;

            if (remainder == 2)
            {
                payload += "==";
            }
            else if (remainder == 3)
            {
                payload += "=";
            }

            byte[] payloadBytes =
                Convert.FromBase64String(
                    payload);

            string json =
                Encoding.UTF8.GetString(
                    payloadBytes);

            using JsonDocument document =
                JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    "exp",
                    out JsonElement expiryElement))
            {
                return null;
            }

            if (!expiryElement.TryGetInt64(
                    out long expirySeconds))
            {
                return null;
            }

            return DateTimeOffset
                .FromUnixTimeSeconds(
                    expirySeconds);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to read JWT expiry: {ex.Message}");

            return null;
        }
    }
}