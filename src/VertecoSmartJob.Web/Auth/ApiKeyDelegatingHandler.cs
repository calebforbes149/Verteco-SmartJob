namespace VertecoSmartJob.Web.Auth;

public class ApiKeyDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string ApiKeyClaimType = "smartjob:apikey";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = httpContextAccessor.HttpContext?.User.FindFirst(ApiKeyClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Remove("X-SmartJob-Key");
            request.Headers.Add("X-SmartJob-Key", apiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}