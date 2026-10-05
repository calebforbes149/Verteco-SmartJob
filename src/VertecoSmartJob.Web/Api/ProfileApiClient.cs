using System.Net.Http.Json;
using VertecoSmartJob.Contracts.Auth;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Api;

public sealed class ProfileApiClient(HttpClient http)
{
    public async Task<UserDto> GetAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("api/v1/account/profile", ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct))!;
    }

    public async Task<UserDto> UpdateAsync(UpdateOwnProfileRequest request, CancellationToken ct = default)
    {
        using var response = await http.PatchAsJsonAsync("api/v1/account/profile", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct))!;
    }
}
