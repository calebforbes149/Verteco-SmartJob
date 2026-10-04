using System.Net.Http.Headers;

namespace SmartJobMobile.Services;

public sealed class AuthenticatedHttpMessageHandler
    : DelegatingHandler
{
    private readonly ITokenStorageService _tokenStorageService;

    public AuthenticatedHttpMessageHandler(
        ITokenStorageService tokenStorageService)
    {
        _tokenStorageService = tokenStorageService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? accessToken =
            await _tokenStorageService.GetAccessTokenAsync();

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }
        else
        {
            request.Headers.Authorization = null;
        }

        return await base.SendAsync(
            request,
            cancellationToken);
    }
}