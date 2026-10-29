using VertecoSmartJob.Application.Reports;

namespace VertecoSmartJob.Infrastructure.Ai;

/// <summary>Uses grounded local insights if the optional external provider is unavailable.</summary>
public sealed class FallbackReportingAiService(IReportingAiService primary, IReportingAiService fallback)
    : IReportingAiService
{
    public async Task<ReportingAiResult> SummariseAsync(ReportStatistics statistics, CancellationToken ct = default)
    {
        var result = await primary.SummariseAsync(statistics, ct);
        if (result.Status == ReportingAiStatus.Available && !string.IsNullOrWhiteSpace(result.Commentary))
            return result;

        return await fallback.SummariseAsync(statistics, ct);
    }
}
