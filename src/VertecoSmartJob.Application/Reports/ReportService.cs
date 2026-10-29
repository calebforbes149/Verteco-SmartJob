using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Application.Reports;

public sealed record ReportStatistics(
    string Id, DateTime FromUtc, DateTime ToUtc, int JobsCreated, int JobsCompleted,
    double? AverageCompletionHours, Dictionary<string, int> StatusCounts,
    int JobsInScope = 0, int FlaggedJobs = 0, int HighPriorityJobs = 0);

public enum ReportingAiStatus { Available, LocalInsights, NotConfigured, RateLimited, Unavailable }

public sealed record ReportingAiResult(string? Commentary, ReportingAiStatus Status);

public interface IReportingAiService
{
    Task<ReportingAiResult> SummariseAsync(ReportStatistics statistics, CancellationToken ct = default);
}

public sealed class ReportService(IJobCardRepository jobs, IReportingAiService ai)
{
    public async Task<ReportStatistics> GetStatisticsAsync(int year, int month, CancellationToken ct = default)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);
        var records = await jobs.GetForReportingAsync(from, to, ct);
        var completed = records.Where(j => j.CompletedAtUtc >= from && j.CompletedAtUtc < to).ToList();
        var durations = completed.Select(j => (j.CompletedAtUtc!.Value - j.CreatedAtUtc).TotalHours)
            .Where(h => h >= 0).ToList();
        return new ReportStatistics(
            from.ToString("yyyy-MM"), from, to,
            records.Count(j => j.CreatedAtUtc >= from && j.CreatedAtUtc < to),
            completed.Count,
            durations.Count == 0 ? null : Math.Round(durations.Average(), 1),
            records.GroupBy(j => j.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            records.Count,
            records.Count(j => j.IsFlagged),
            records.Count(j => j.Priority is JobPriority.High or JobPriority.Urgent));
    }

    public async Task<(ReportStatistics Statistics, ReportingAiResult Ai)> GetReportAsync(int year, int month, CancellationToken ct = default)
    {
        var statistics = await GetStatisticsAsync(year, month, ct);
        return (statistics, await ai.SummariseAsync(statistics, ct));
    }
}
