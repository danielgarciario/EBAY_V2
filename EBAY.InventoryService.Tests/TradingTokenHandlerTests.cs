using EBAYHttpClient.Handler;
using EBAYHttpClient.Services;
using System.Net;

namespace EBAY.InventoryService.Tests;

public sealed class TradingTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsTradingTokenHeaderWithoutBearerAuthorization()
    {
        var capture = new HeaderCaptureHandler();
        var handler = new EBAYTradingTokenDelegateHandler(new FakeTokenService())
        {
            InnerHandler = capture
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://api.ebay.com/ws/api.dll");

        Assert.Equal("test-token", capture.TradingToken);
        Assert.Null(capture.Authorization);
    }

    private sealed class FakeTokenService : IOAuthTokenService
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("test-token");
    }

    private sealed class HeaderCaptureHandler : HttpMessageHandler
    {
        public string? TradingToken { get; private set; }
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            TradingToken = request.Headers.GetValues("X-EBAY-API-IAF-TOKEN").Single();
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
