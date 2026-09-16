using EBAYHttpClient.Services;

namespace EBAYHttpClient.Handler;

public sealed class EBAYHeadersDelegateHandler : DelegatingHandler
{
    private readonly IOAuthTokenService tokenservice;

    public EBAYHeadersDelegateHandler(IOAuthTokenService tokenservice)
    {
        this.tokenservice = tokenservice;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenservice.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
