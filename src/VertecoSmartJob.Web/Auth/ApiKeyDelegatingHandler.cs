using System.Net.Http.Headers;

namespace VertecoSmartJob.Web.Auth;

public class ApiKeyDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string AccessTokenClaimType = "smartjob:accesstoken";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}