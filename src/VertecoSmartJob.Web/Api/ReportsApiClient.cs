using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reports;

namespace VertecoSmartJob.Web.Api;

public class ReportsApiClient(HttpClient http)
{
    public async Task<PagedResponse<ReportDto>> SearchAsync(int? year = null, int page = 1, int pageSize = 12, string sort = "-month", CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString("api/v1/reports", new Dictionary<string, string?>
        {
            ["year"] = year?.ToString(),
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["sort"] = sort
        });
        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<ReportDto>>(cancellationToken: ct))!;
    }

    public async Task<ReportDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/reports/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<ReportDto>(cancellationToken: ct);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> ExportAsync(string id, string format, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/reports/{id}/export?format={Uri.EscapeDataString(format)}", ct);
        await response.EnsureApiSuccessAsync(ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? $"smartjob-report-{id}.{format}";
        return (bytes, contentType, fileName.Trim('"'));
    }
}