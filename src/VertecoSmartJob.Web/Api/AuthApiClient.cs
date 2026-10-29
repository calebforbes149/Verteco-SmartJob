using System.Net.Http.Json;
using VertecoSmartJob.Contracts.Auth;

namespace VertecoSmartJob.Web.Api;

public class AuthApiClient(HttpClient http)
{
    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest { Email = email, Password = password }, ct);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden)
            return null;

        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync(
            "api/v1/auth/forgot-password", new ForgotPasswordRequest { Email = email }, ct);
        await response.EnsureApiSuccessAsync(ct);
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync(
            "api/v1/auth/reset-password",
            new ResetPasswordRequest { Token = token, NewPassword = newPassword }, ct);
        await response.EnsureApiSuccessAsync(ct);
    }
}
