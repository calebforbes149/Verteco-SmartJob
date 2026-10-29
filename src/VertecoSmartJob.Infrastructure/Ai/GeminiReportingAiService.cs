using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VertecoSmartJob.Application.Reports;

namespace VertecoSmartJob.Infrastructure.Ai;

public sealed class GeminiReportingAiService(HttpClient http, IConfiguration config, ILogger<GeminiReportingAiService> logger)
    : IReportingAiService
{
    private readonly ConcurrentDictionary<string, (ReportingAiResult Result, DateTime ExpiresUtc)> cache = new();

    public async Task<ReportingAiResult> SummariseAsync(ReportStatistics statistics, CancellationToken ct = default)
    {
        var key = config["Ai:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            key = config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            return new ReportingAiResult(null, ReportingAiStatus.NotConfigured);

        var model = config["Ai:Model"] ?? config["Gemini:Model"] ?? "gemini-3.5-flash-lite";
        if (!Regex.IsMatch(model, "^[a-zA-Z0-9.-]+$"))
            return new ReportingAiResult(null, ReportingAiStatus.Unavailable);

        // The cache follows the actual aggregates, so new report data gets fresh commentary.
        var cacheKey = JsonSerializer.Serialize(new
        {
            Model = model, statistics.Id, statistics.JobsCreated, statistics.JobsCompleted,
            statistics.AverageCompletionHours,
            statistics.JobsInScope, statistics.FlaggedJobs, statistics.HighPriorityJobs,
            StatusCounts = statistics.StatusCounts.OrderBy(pair => pair.Key).ToArray()
        });
        if (cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresUtc > DateTime.UtcNow)
            return cached.Result;

        var prompt = "You are assisting a field-service manager. Analyse this monthly SmartJob report " +
            "and write useful operational insight, not a generic summary. Use exactly these headings " +
            "on separate lines: Monthly overview, Workflow bottlenecks, Risk and priority, " +
            "Recommended actions, Data limitations. Use 120 to 180 words in plain text. " +
            "Under Recommended actions, give two specific, numbered actions based on the supplied counts. " +
            "Discuss the largest active status group if one exists, flagged work, high or urgent work, " +
            "and average completion time when available. Distinguish observations from recommendations. " +
            "Use only the supplied aggregate numbers. Never invent trends, causes, people, percentages, " +
            "forecasts, or completion rates. Created and completed jobs can be different groups. " +
            "Status, flag, and priority counts describe the current state of jobs created or " +
            "completed in the period, not a month-end snapshot. If a metric is zero or unknown, " +
            "say so without suggesting a cause. Do not imply that a suggested action is already done. " +
            $"Month={statistics.Id}; created={statistics.JobsCreated}; completed={statistics.JobsCompleted}; " +
            $"jobsInScope={statistics.JobsInScope}; flagged={statistics.FlaggedJobs}; " +
            $"highOrUrgentPriority={statistics.HighPriorityJobs}; " +
            $"averageCompletionHours={statistics.AverageCompletionHours?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"}; " +
            $"statusCounts={JsonSerializer.Serialize(statistics.StatusCounts)}.";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");
            request.Headers.Add("x-goog-api-key", key);
            request.Content = JsonContent.Create(new { contents = new[] { new { parts = new[] { new { text = prompt } } } } });
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Reporting AI request failed with HTTP {StatusCode} for model {Model}", (int)response.StatusCode, model);
                var failure = new ReportingAiResult(null,
                    response.StatusCode == HttpStatusCode.TooManyRequests ? ReportingAiStatus.RateLimited : ReportingAiStatus.Unavailable);
                cache[cacheKey] = (failure, DateTime.UtcNow.AddMinutes(5));
                return failure;
            }

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var result = string.Join("\n", json.RootElement.GetProperty("candidates")[0]
                .GetProperty("content").GetProperty("parts").EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text)));
            if (string.IsNullOrWhiteSpace(result))
                return new ReportingAiResult(null, ReportingAiStatus.Unavailable);

            var success = new ReportingAiResult(result.Trim(), ReportingAiStatus.Available);
            cache[cacheKey] = (success, DateTime.UtcNow.AddHours(6));
            return success;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested &&
            ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            logger.LogWarning("Reporting AI unavailable: {Reason}", ex.GetType().Name);
            return new ReportingAiResult(null, ReportingAiStatus.Unavailable);
        }
    }
}
