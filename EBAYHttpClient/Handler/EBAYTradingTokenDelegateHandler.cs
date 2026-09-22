using EBAYHttpClient.Services;

namespace EBAYHttpClient.Handler;

public sealed class EBAYTradingTokenDelegateHandler(IOAuthTokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        request.Headers.Remove("X-EBAY-API-IAF-TOKEN");
        request.Headers.Add("X-EBAY-API-IAF-TOKEN", token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
