using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace VertecoSmartJob.Web.Api;

public static class ApiResponseExtensions
{
    public static async Task EnsureApiSuccessAsync(this HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode) return;

        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
        }
        catch
        {
        }

        var retryAfter = response.Headers.RetryAfter;
        var remaining = retryAfter?.Delta ??
            (retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : (TimeSpan?)null);
        var retryAfterSeconds = remaining is { } duration
            ? Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds))
            : (int?)null;

        throw new ApiException(
            (int)response.StatusCode,
            problem?.Title ?? $"Request failed ({(int)response.StatusCode} {response.StatusCode})",
            problem?.Detail,
            retryAfterSeconds);
    }
}
