using System.Net.Http.Json;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Api;

public class JobTemplatesApiClient(HttpClient http)
{
    public async Task<JobTemplateDto?> GetStandardAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/v1/jobtemplates/standard", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await response.EnsureApiSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<JobTemplateDto>(cancellationToken: ct);
    }
}