using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Web.Api;

public class CorrectiveActionsApiClient(HttpClient http)
{
    public async Task<PagedResponse<CorrectiveActionDto>> SearchAsync(
        string? status = null, Guid? jobId = null, string? q = null,
        int page = 1, int pageSize = 20, string sort = "-date", CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString("api/v1/corrective-actions", new Dictionary<string, string?>
        {
            ["status"] = status,
            ["jobId"] = jobId?.ToString(),
            ["q"] = q,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["sort"] = sort
        });
        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<CorrectiveActionDto>>(cancellationToken: ct))!;
    }

    public async Task<CorrectiveActionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/corrective-actions/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<CorrectiveActionDto>(cancellationToken: ct);
    }

    public async Task<CorrectiveActionDto> ResolveAsync(Guid id, string resolutionNotes, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/corrective-actions/{id}/resolve",
            new ResolveFlagRequest { ResolutionNotes = resolutionNotes }, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<CorrectiveActionDto>(cancellationToken: ct))!;
    }
}