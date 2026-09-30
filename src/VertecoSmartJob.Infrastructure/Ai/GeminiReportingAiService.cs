using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VertecoSmartJob.Application.Reports;

namespace VertecoSmartJob.Infrastructure.Ai;

public sealed class GeminiReportingAiService(HttpClient http, IConfiguration config, ILogger<GeminiReportingAiService> logger)
    : IReportingAiService
{
    public async Task<string?> SummariseAsync(ReportStatistics statistics, CancellationToken ct = default)
    {
        var key = config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;

        var model = config["Gemini:Model"] ?? "gemini-3.8-flash";
        if (!System.Text.RegularExpressions.Regex.IsMatch(model, "^[a-zA-Z0-9.-]+$")) return null;
        var prompt = $"Write at most two factual sentences about these aggregate Verteco job metrics. " +
            $"Do not invent trends or mention names. Month={statistics.Id}; created={statistics.JobsCreated}; " +
            $"completed={statistics.JobsCompleted}; " +
            $"averageCompletionHours={statistics.AverageCompletionHours?.ToString() ?? "unknown"}; " +
            $"statusCounts={JsonSerializer.Serialize(statistics.StatusCounts)}.";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");
            request.Headers.Add("x-goog-api-key", key);
            request.Content = JsonContent.Create(new { contents = new[] { new { parts = new[] { new { text = prompt } } } } });
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var candidates = json.RootElement.GetProperty("candidates");
            var result = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            logger.LogWarning("Reporting AI unavailable: {Reason}", ex.GetType().Name);
            return null;
        }
    }
}
