using System.Net.Http.Json;
using VertecoSmartJob.Contracts.Auth;

namespace VertecoSmartJob.Web.Api;

public class AuthApiClient(HttpClient http)
{
    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest { Email = email, Password = password }, ct);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Locked
            or System.Net.HttpStatusCode.Forbidden)
            return null;   // bad credentials, locked out, or unconfirmed - caller shows a generic message

        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
    }
}