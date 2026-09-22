using EBAY.InventoryService.InventoryUpdate;
using EBAY.InventoryService.InventoryUpdate.Data;
using EBAYHttpClient;
using EBAYHttpClient.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace EBAY.InventoryService.Tests;

public sealed class InventoryUpdateServiceTests
{
    [Fact]
    public async Task UpdateInventoryAsync_WhenNoDifferences_DoesNotCallEbay()
    {
        var handler = new QueueHttpMessageHandler();
        var service = CreateService([], handler);

        var result = await service.UpdateInventoryAsync();

        Assert.Equal(InventoryUpdateRunStatus.NoChanges, result.Status);
        Assert.Empty(handler.RequestBodies);
    }

    [Fact]
    public async Task UpdateInventoryAsync_SplitsFiveLinesIntoGroupsOfFourAndOne()
    {
        var items = CreateItems(5);
        var handler = new QueueHttpMessageHandler(
            SuccessResponse("one"),
            SuccessResponse("two"));
        var service = CreateService(items, handler);

        var result = await service.UpdateInventoryAsync();

        Assert.Equal(InventoryUpdateRunStatus.Completed, result.Status);
        Assert.Equal(5, result.SubmittedLines);
        Assert.Equal(5, result.SuccessfulLines);
        Assert.Equal(2, result.Batches.Count);
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Equal(4, CountInventoryStatuses(handler.RequestBodies[0]));
        Assert.Equal(1, CountInventoryStatuses(handler.RequestBodies[1]));
    }

    [Fact]
    public async Task UpdateInventoryAsync_ContinuesAfterFailedGroup()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Fehler")
            },
            SuccessResponse("second"));
        var service = CreateService(CreateItems(5), handler);

        var result = await service.UpdateInventoryAsync();

        Assert.Equal(InventoryUpdateRunStatus.PartiallyFailed, result.Status);
        Assert.Equal(4, result.FailedLines);
        Assert.Equal(1, result.SuccessfulLines);
        Assert.Equal(2, handler.RequestBodies.Count);
    }

    [Fact]
    public async Task UpdateInventoryAsync_SkipsNegativeAndDuplicateLines()
    {
        var items = new[]
        {
            new InventoryUpdateItem("1", "SKU-1", 1, 2),
            new InventoryUpdateItem("1", "SKU-1", 1, 3),
            new InventoryUpdateItem("2", "SKU-2", 1, -1)
        };
        var handler = new QueueHttpMessageHandler(SuccessResponse("valid"));
        var service = CreateService(items, handler);

        var result = await service.UpdateInventoryAsync();

        Assert.Equal(InventoryUpdateRunStatus.PartiallyFailed, result.Status);
        Assert.Equal(2, result.InvalidLines);
        Assert.Equal(1, result.SubmittedLines);
        Assert.Equal(1, CountInventoryStatuses(Assert.Single(handler.RequestBodies)));
    }

    [Theory]
    [InlineData("Warning", InventoryUpdateRunStatus.CompletedWithWarnings, 2, 0)]
    [InlineData("Failure", InventoryUpdateRunStatus.Failed, 0, 2)]
    public async Task UpdateInventoryAsync_MapsWarningAndFailureAcknowledgements(
        string ack,
        InventoryUpdateRunStatus expectedStatus,
        int expectedWarnings,
        int expectedFailures)
    {
        var handler = new QueueHttpMessageHandler(AckResponse(ack));
        var service = CreateService(CreateItems(2), handler);

        var result = await service.UpdateInventoryAsync();

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedWarnings, result.WarningLines);
        Assert.Equal(expectedFailures, result.FailedLines);
    }

    [Fact]
    public async Task UpdateInventoryAsync_MapsReturnedLinesFromPartialFailure()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                <ReviseInventoryStatusResponse xmlns="urn:ebay:apis:eBLBaseComponents">
                  <Ack>PartialFailure</Ack>
                  <InventoryStatus><SKU>SKU-1</SKU><ItemID>1</ItemID></InventoryStatus>
                  <Errors><ErrorCode>17</ErrorCode><SeverityCode>Error</SeverityCode></Errors>
                </ReviseInventoryStatusResponse>
                """)
        };
        var service = CreateService(CreateItems(2), new QueueHttpMessageHandler(response));

        var result = await service.UpdateInventoryAsync();

        Assert.Equal(InventoryUpdateRunStatus.PartiallyFailed, result.Status);
        Assert.Equal(1, result.SuccessfulLines);
        Assert.Equal(1, result.FailedLines);
        Assert.Equal("17", Assert.Single(Assert.Single(result.Batches).EbayErrors).ErrorCode);
    }

    [Fact]
    public async Task UpdateInventoryAsync_ContinuesAfterMalformedXml()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-xml") },
            SuccessResponse("second"));
        var service = CreateService(CreateItems(5), handler);

        var result = await service.UpdateInventoryAsync();

        Assert.Equal(InventoryUpdateRunStatus.PartiallyFailed, result.Status);
        Assert.Equal(4, result.FailedLines);
        Assert.Equal(1, result.SuccessfulLines);
        Assert.Equal(2, handler.RequestBodies.Count);
    }

    [Fact]
    public async Task UpdateInventoryAsync_PropagatesUserCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = CreateService(CreateItems(1), new QueueHttpMessageHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.UpdateInventoryAsync(cancellation.Token));
    }

    [Fact]
    public async Task UpdateInventoryAsync_WhenAnotherRunOwnsLock_ReturnsAlreadyRunning()
    {
        var blockingSource = new BlockingSource();
        var handler = new QueueHttpMessageHandler();
        var sharedLock = new InventoryUpdateExecutionLock();
        var firstService = CreateService(blockingSource, handler, sharedLock);
        var secondService = CreateService([], handler, sharedLock);

        var firstRun = firstService.UpdateInventoryAsync();
        await blockingSource.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var secondResult = await secondService.UpdateInventoryAsync();
        blockingSource.Release.SetResult();
        await firstRun;

        Assert.Equal(InventoryUpdateRunStatus.AlreadyRunning, secondResult.Status);
    }

    private static InventoryUpdateService CreateService(
        IReadOnlyList<InventoryUpdateItem> items,
        QueueHttpMessageHandler handler,
        InventoryUpdateExecutionLock? executionLock = null) =>
        CreateService(new FakeSource(items), handler, executionLock);

    private static InventoryUpdateService CreateService(
        IInventoryUpdateSource source,
        QueueHttpMessageHandler handler,
        InventoryUpdateExecutionLock? executionLock = null)
    {
        var options = Options.Create(new EBAYClientOptions
        {
            Appid = "app",
            Certid = "cert",
            Devid = "dev",
            ClientBaseUrl = "https://api.ebay.com",
            EbayAuthAPIBaseUrl = "https://api.ebay.com",
            AuthToken = "refresh",
            APIVersion = "1",
            Scopes = ["scope"],
            ContentLanguage = "de-DE",
            EbayMarketPlaceID = "77",
            TradingAPIVersion = "1477"
        });

        return new InventoryUpdateService(
            NullLogger<InventoryUpdateService>.Instance,
            source,
            new FakeHttpClientFactory(handler),
            options,
            executionLock ?? new InventoryUpdateExecutionLock());
    }

    private static InventoryUpdateItem[] CreateItems(int count) =>
        Enumerable.Range(1, count)
            .Select(number => new InventoryUpdateItem(number.ToString(), $"SKU-{number}", 0, number))
            .ToArray();

    private static HttpResponseMessage SuccessResponse(string correlationId) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent($"""
                <ReviseInventoryStatusResponse xmlns="urn:ebay:apis:eBLBaseComponents">
                  <Ack>Success</Ack><CorrelationID>{correlationId}</CorrelationID>
                </ReviseInventoryStatusResponse>
                """)
        };

    private static HttpResponseMessage AckResponse(string ack) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent($"""
                <ReviseInventoryStatusResponse xmlns="urn:ebay:apis:eBLBaseComponents">
                  <Ack>{ack}</Ack>
                </ReviseInventoryStatusResponse>
                """)
        };

    private static int CountInventoryStatuses(string xml) =>
        xml.Split("<InventoryStatus>", StringSplitOptions.None).Length - 1;

    private sealed class FakeSource(IReadOnlyList<InventoryUpdateItem> items) : IInventoryUpdateSource
    {
        public Task<IReadOnlyList<InventoryUpdateItem>> GetDifferencesAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(items);
    }

    private sealed class BlockingSource : IInventoryUpdateSource
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<InventoryUpdateItem>> GetDifferencesAsync(
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return [];
        }
    }

    private sealed class FakeHttpClientFactory(QueueHttpMessageHandler handler) : IEBayHttpClientFactory
    {
        public HttpClient GetEbayHttpClient() => CreateClient();
        public HttpClient GetEbayTradingApiHttpClient() => CreateClient();

        private HttpClient CreateClient() => new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.ebay.com/ws/api.dll")
        };
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses;

        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            this.responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (responses.Count == 0)
            {
                throw new InvalidOperationException("Für diesen Test wurde keine HTTP-Antwort konfiguriert.");
            }

            return responses.Dequeue();
        }
    }
}
