using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VertecoSmartJob.Contracts.Common;

namespace VertecoSmartJob.Web.Api;

public class AuditApiClient(HttpClient http)
{
    public async Task<PagedResponse<AuditEntryDto>> SearchAsync(
        int page = 1, int pageSize = 20, Guid? jobId = null, string? q = null, string sort = "-date", CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString("api/v1/audit", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["jobId"] = jobId?.ToString(),
            ["q"] = q,
            ["sort"] = sort
        });
        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<AuditEntryDto>>(cancellationToken: ct))!;
    }
}