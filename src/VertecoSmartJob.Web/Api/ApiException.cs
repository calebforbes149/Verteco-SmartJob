namespace VertecoSmartJob.Web.Api;

public class ApiException(int statusCode, string title, string? detail) : Exception(detail ?? title)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string? Detail { get; } = detail;
}