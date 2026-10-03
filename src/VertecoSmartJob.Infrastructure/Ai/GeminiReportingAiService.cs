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
        var key = config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            return new ReportingAiResult(null, ReportingAiStatus.NotConfigured);

        var model = config["Gemini:Model"] ?? "gemini-3.8-flash";
        if (!Regex.IsMatch(model, "^[a-zA-Z0-9.-]+$"))
            return new ReportingAiResult(null, ReportingAiStatus.Unavailable);

        // The cache follows the actual aggregates, so new report data gets fresh commentary.
        var cacheKey = JsonSerializer.Serialize(new
        {
            Model = model, statistics.Id, statistics.JobsCreated, statistics.JobsCompleted,
            statistics.AverageCompletionHours,
            StatusCounts = statistics.StatusCounts.OrderBy(pair => pair.Key).ToArray()
        });
        if (cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresUtc > DateTime.UtcNow)
            return cached.Result;

        var prompt = $"Write at most two factual sentences about these aggregate Verteco job metrics. " +
            $"Do not invent trends or mention names. Month={statistics.Id}; created={statistics.JobsCreated}; " +
            $"completed={statistics.JobsCompleted}; " +
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
            var result = json.RootElement.GetProperty("candidates")[0].GetProperty("content")
                .GetProperty("parts")[0].GetProperty("text").GetString();
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
