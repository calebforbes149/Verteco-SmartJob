using System.Net.Http.Json;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Api;

public class DashboardApiClient(HttpClient http)
{
    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/v1/dashboard/stats", ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<DashboardStatsDto>(cancellationToken: ct))!;
    }

    public async Task<List<JobCardSummaryDto>> GetRecentJobsAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/v1/dashboard/recent-jobs", ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<List<JobCardSummaryDto>>(cancellationToken: ct))!;
    }

    public async Task<List<JobCardSummaryDto>> GetStatusBoardAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/v1/dashboard/status-board", ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<List<JobCardSummaryDto>>(cancellationToken: ct))!;
    }
}
