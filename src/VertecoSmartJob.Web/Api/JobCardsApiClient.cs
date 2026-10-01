using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Web.Api;

public class JobCardsApiClient(HttpClient http)
{
    public async Task<PagedResponse<JobCardSummaryDto>> SearchAsync(JobCardListQuery query, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = query.Page.ToString(),
            ["pageSize"] = query.PageSize.ToString(),
            ["sort"] = query.Sort,
            ["q"] = query.Q,
            ["status"] = query.Status,
            ["priority"] = query.Priority,
            ["customerId"] = query.CustomerId?.ToString(),
            ["siteId"] = query.SiteId?.ToString(),
            ["technicianId"] = query.TechnicianId?.ToString(),
            ["flagged"] = query.Flagged?.ToString()
        };
        var url = QueryHelpers.AddQueryString("api/v1/jobcards", parameters);

        var response = await http.GetAsync(url, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<JobCardSummaryDto>>(cancellationToken: ct))!;
    }

    public async Task<JobCardDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/v1/jobcards/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct);
    }

    public async Task<JobCardDetailDto> CreateAsync(CreateJobCardRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/jobcards", request, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct))!;
    }

    public async Task<JobCardDetailDto> AssignAsync(Guid id, Guid technicianId, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/jobcards/{id}/assign",
            new AssignTechnicianRequest { TechnicianId = technicianId }, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct))!;
    }

    public async Task<JobCardDetailDto> CompleteAsync(Guid id, string? reviewNotes, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/jobcards/{id}/complete",
            new CompleteJobRequest { ReviewNotes = reviewNotes }, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct))!;
    }

    public async Task<JobCardDetailDto> ReopenAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/jobcards/{id}/reopen",
            new ReopenJobRequest { Reason = reason }, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct))!;
    }

    public async Task<JobCardDetailDto> CancelAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/jobcards/{id}/cancel",
            new CancelJobRequest { Reason = reason }, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct))!;
    }

    public async Task<JobCardDetailDto> FlagAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/jobcards/{id}/flag",
            new FlagJobRequest { Reason = reason }, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct))!;
    }

    public async Task<JobCardDetailDto> ResolveFlagAsync(Guid id, string resolutionNotes, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/v1/jobcards/{id}/resolve-flag",
            new ResolveFlagRequest { ResolutionNotes = resolutionNotes }, ct);
        await response.EnsureApiSuccessAsync(ct);
        return (await response.Content.ReadFromJsonAsync<JobCardDetailDto>(cancellationToken: ct))!;
    }
}