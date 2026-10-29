using System.Globalization;

namespace VertecoSmartJob.Application.Reports;

/// <summary>
/// A no-cost, deterministic report analysis. It is deliberately identified as
/// local insights so managers never mistake it for a response from an AI model.
/// </summary>
public sealed class LocalReportingInsightsService : IReportingAiService
{
    public Task<ReportingAiResult> SummariseAsync(ReportStatistics statistics, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var lines = new List<string>
        {
            "Monthly overview",
            $"{statistics.JobsCreated} jobs were created and {statistics.JobsCompleted} were completed in {statistics.Id}."
        };

        lines.Add("Delivery time");
        lines.Add(statistics.AverageCompletionHours is { } hours
            ? $"Completed jobs took {hours.ToString("0.0", CultureInfo.InvariantCulture)} hours on average from creation to completion."
            : "No average completion time is available for this month.");

        lines.Add("Attention points");
        lines.Add($"Among the {statistics.JobsInScope} jobs created or completed this month, " +
                  $"{statistics.FlaggedJobs} are currently flagged and {statistics.HighPriorityJobs} have high or urgent priority.");

        var active = statistics.StatusCounts
            .Where(pair => pair.Key is not ("Completed" or "Cancelled"))
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .FirstOrDefault();
        if (active.Value > 0)
            lines.Add($"The largest current active status in this group is {active.Key} ({active.Value} jobs).");

        lines.Add("Suggested follow-up");
        lines.Add(statistics.FlaggedJobs > 0
            ? "Review flagged jobs and their corrective actions before approving work."
            : statistics.StatusCounts.GetValueOrDefault("AwaitingApproval") > 0
                ? "Review jobs awaiting approval and confirm their evidence and signatures."
                : statistics.StatusCounts.GetValueOrDefault("Reopened") > 0
                    ? "Check reopened jobs for outstanding corrections."
                    : statistics.JobsInScope > 0
                        ? "Review the current assignments and scheduled work for the next period."
                        : "There is not enough activity in this period to suggest a workflow action.");

        lines.Add("How to read this report");
        lines.Add("Created and completed jobs can be different groups, so their counts are not a completion rate. " +
                  "Status and attention counts describe the current state of jobs created or completed in this period, not a historical month-end snapshot.");

        return Task.FromResult(new ReportingAiResult(string.Join(Environment.NewLine, lines), ReportingAiStatus.LocalInsights));
    }
}
