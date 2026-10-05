using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Api;

public class CustomersApiClient(HttpClient http)
{
    public async Task<PagedResponse<CustomerDto>> SearchAsync(string? q = null, int page = 1, int pageSize = 20, string sort = "name", CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString("api/v1/customers", new Dictionary<string, string?>
        {
            ["q"] = q,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["sort"] = sort
        });
        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<CustomerDto>>(cancellationToken: ct))!;
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/customers/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: ct);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/customers", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: ct))!;
    }

    public async Task<CustomerDto> PatchAsync(Guid id, PatchCustomerRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/v1/customers/{id}", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: ct))!;
    }
}