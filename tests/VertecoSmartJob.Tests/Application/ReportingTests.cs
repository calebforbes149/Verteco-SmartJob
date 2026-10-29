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

        var result = await ai.SummariseAsync(stats);
        Assert.Equal(ReportingAiStatus.NotConfigured, result.Status);
        Assert.Null(result.Commentary);
        Assert.Equal(5, stats.JobsCompleted);
    }

    [Fact]
    public async Task GeminiSuccess_IsCachedForUnchangedReportStatistics()
    {
        var calls = 0;
        Uri? requestUri = null;
        string? requestBody = null;
        string? sentKey = null;
        using var client = new HttpClient(new StubHandler(request =>
        {
            calls++;
            requestUri = request.RequestUri;
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            sentKey = request.Headers.GetValues("x-goog-api-key").Single();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Five jobs were completed.\"}]}}]}")
            };
        }));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ai:ApiKey"] = "",
            ["Gemini:ApiKey"] = "test-key",
            ["Ai:Model"] = "gemini-3.5-flash-lite",
            ["Gemini:Model"] = "gemini-3.8-flash"
        }).Build();
        var ai = new GeminiReportingAiService(client, config, NullLogger<GeminiReportingAiService>.Instance);
        var stats = new ReportStatistics("2026-09", new DateTime(2026, 9, 1), new DateTime(2026, 10, 1),
            8, 5, 24.5, new Dictionary<string, int> { ["Completed"] = 5 });

        var first = await ai.SummariseAsync(stats);
        var second = await ai.SummariseAsync(stats);

        Assert.Equal(ReportingAiStatus.Available, first.Status);
        Assert.Equal("Five jobs were completed.", first.Commentary);
        Assert.Equal(first, second);
        Assert.Equal(1, calls);
        Assert.Equal("test-key", sentKey);
        Assert.Contains("gemini-3.5-flash-lite", requestUri!.AbsoluteUri);
        Assert.Contains("Workflow bottlenecks", requestBody);
        Assert.Contains("Recommended actions", requestBody);
    }

    [Fact]
    public async Task GeminiRateLimit_IsReportedWithoutBlockingStatistics()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-key"
        }).Build();
        var ai = new GeminiReportingAiService(client, config, NullLogger<GeminiReportingAiService>.Instance);
        var stats = new ReportStatistics("2026-09", new DateTime(2026, 9, 1), new DateTime(2026, 10, 1),
            8, 5, 24.5, new Dictionary<string, int> { ["Completed"] = 5 });

        var result = await ai.SummariseAsync(stats);
        Assert.Equal(ReportingAiStatus.RateLimited, result.Status);
        Assert.Null(result.Commentary);
        Assert.Equal(5, stats.JobsCompleted);
    }

    [Fact]
    public async Task LocalInsights_AreGroundedInTheReportAndIdentifyTheirLimitations()
    {
        var stats = new ReportStatistics("2026-09", new DateTime(2026, 9, 1), new DateTime(2026, 10, 1),
            8, 5, 24.5, new Dictionary<string, int> { ["Completed"] = 5, ["AwaitingApproval"] = 2 },
            JobsInScope: 10, FlaggedJobs: 2, HighPriorityJobs: 3);

        var result = await new LocalReportingInsightsService().SummariseAsync(stats);

        Assert.Equal(ReportingAiStatus.LocalInsights, result.Status);
        Assert.Contains("2 are currently flagged", result.Commentary);
        Assert.Contains("AwaitingApproval (2 jobs)", result.Commentary);
        Assert.Contains("not a completion rate", result.Commentary);
    }

    [Fact]
    public async Task GeminiRateLimit_FallsBackToLocalInsights()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ai:ApiKey"] = "test-key"
        }).Build();
        var primary = new GeminiReportingAiService(client, config, NullLogger<GeminiReportingAiService>.Instance);
        var ai = new FallbackReportingAiService(primary, new LocalReportingInsightsService());
        var stats = new ReportStatistics("2026-09", new DateTime(2026, 9, 1), new DateTime(2026, 10, 1),
            8, 5, 24.5, new Dictionary<string, int> { ["Completed"] = 5 }, JobsInScope: 8);

        var result = await ai.SummariseAsync(stats);

        Assert.Equal(ReportingAiStatus.LocalInsights, result.Status);
        Assert.Contains("Monthly overview", result.Commentary);
        Assert.Equal(5, stats.JobsCompleted);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
