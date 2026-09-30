using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VertecoSmartJob.Application.Reports;
using VertecoSmartJob.Infrastructure.Ai;

namespace VertecoSmartJob.Tests.Application;

public class ReportingTests
{
    [Fact]
    public async Task NoConfiguredAiKey_LeavesStatisticsAvailable()
    {
        var ai = new GeminiReportingAiService(new HttpClient(),
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            NullLogger<GeminiReportingAiService>.Instance);
        var stats = new ReportStatistics("2026-09", new DateTime(2026, 9, 1), new DateTime(2026, 10, 1),
            8, 5, 24.5, new Dictionary<string, int> { ["Completed"] = 5 });

        Assert.Null(await ai.SummariseAsync(stats));
        Assert.Equal(5, stats.JobsCompleted);
    }
}
