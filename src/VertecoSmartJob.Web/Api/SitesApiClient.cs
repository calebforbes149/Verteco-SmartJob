using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Api;

public class SitesApiClient(HttpClient http)
{
    public async Task<PagedResponse<SiteDto>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString("api/v1/sites", new Dictionary<string, string?>
        {
            ["customerId"] = customerId.ToString(),
            ["pageSize"] = "200"
        });
        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<SiteDto>>(cancellationToken: ct))!;
    }

    public async Task<PagedResponse<SiteDto>> SearchAsync(string? q = null, Guid? customerId = null, int page = 1, int pageSize = 20, string sort = "name", CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString("api/v1/sites", new Dictionary<string, string?>
        {
            ["q"] = q,
            ["customerId"] = customerId?.ToString(),
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["sort"] = sort
        });
        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<SiteDto>>(cancellationToken: ct))!;
    }

    public async Task<SiteDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/sites/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<SiteDto>(cancellationToken: ct);
    }

    public async Task<SiteDto> CreateAsync(CreateSiteRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/sites", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<SiteDto>(cancellationToken: ct))!;
    }

    public async Task<SiteDto> PatchAsync(Guid id, PatchSiteRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/v1/sites/{id}", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<SiteDto>(cancellationToken: ct))!;
    }

    public async Task<EquipmentDto?> FindEquipmentBySerialAsync(string serialNumber, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/sites/equipment/by-serial/{Uri.EscapeDataString(serialNumber)}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<EquipmentDto>(cancellationToken: ct);
    }
}