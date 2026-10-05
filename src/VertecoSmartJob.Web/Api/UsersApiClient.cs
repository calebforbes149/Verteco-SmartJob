using System.Net.Http.Json;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Contracts.Auth;

namespace VertecoSmartJob.Web.Api;

public class UsersApiClient(HttpClient http)
{
    public async Task SetPasswordAsync(Guid id, string newPassword, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/auth/users/{id}/set-password",
            new SetPasswordRequest { NewPassword = newPassword }, ct);
        await response.EnsureApiSuccessAsync(ct);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/users/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
    }

    public async Task<UserDto> SuspendAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/v1/users/{id}/suspend", null, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct))!;
    }

    public async Task<UserDto> ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/v1/users/{id}/reactivate", null, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct))!;
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/v1/users/{id}/deactivate", null, ct);
        await response.EnsureApiSuccessAsync(ct);
    }
}