namespace VertecoSmartJob.Web.Api;

public class ApiException(int statusCode, string title, string? detail, int? retryAfterSeconds = null) : Exception(detail ?? title)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string? Detail { get; } = detail;
    public int? RetryAfterSeconds { get; } = retryAfterSeconds;
}
