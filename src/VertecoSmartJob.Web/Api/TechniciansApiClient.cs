using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Api;

public class TechniciansApiClient(HttpClient http)
{
    public async Task<PagedResponse<TechnicianDto>> SearchAsync(string? q = null, int page = 1, int pageSize = 50, string sort = "name", CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString("api/v1/technicians", new Dictionary<string, string?>
        {
            ["q"] = q,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["sort"] = sort
        });
        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<TechnicianDto>>(cancellationToken: ct))!;
    }

    public async Task<TechnicianDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/technicians/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<TechnicianDto>(cancellationToken: ct);
    }

    public async Task<TechnicianDto> CreateAsync(CreateTechnicianRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/technicians", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<TechnicianDto>(cancellationToken: ct))!;
    }

    public async Task<TechnicianDto> PatchAsync(Guid id, PatchTechnicianRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/v1/technicians/{id}", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<TechnicianDto>(cancellationToken: ct))!;
    }
}