using EBAY.InventoryService.InventoryUpdate;
using EBAY.InventoryService.InventoryUpdate.Data;
using System.Xml.Linq;

namespace EBAY.InventoryService.Tests;

public sealed class InventoryUpdateRequestsTests
{
    [Fact]
    public async Task CreateReviseInventoryStatus_BuildsExpectedHeadersAndEscapedXml()
    {
        var items = new[]
        {
            new InventoryUpdateItem("123", "A&B<1>.St", 3, 7)
        };

        using var request = InventoryUpdateRequests.CreateReviseInventoryStatus(
            items,
            "message-1",
            "1477",
            "77");

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("ReviseInventoryStatus", request.Headers.GetValues("X-EBAY-API-CALL-NAME").Single());
        Assert.Equal("1477", request.Headers.GetValues("X-EBAY-API-COMPATIBILITY-LEVEL").Single());
        Assert.Equal("77", request.Headers.GetValues("X-EBAY-API-SITEID").Single());
        Assert.Equal("text/xml", request.Content!.Headers.ContentType!.MediaType);

        var xml = await request.Content.ReadAsStringAsync();
        var document = XDocument.Parse(xml);
        XNamespace ns = EbayTradingXmlNamespaces.Components;

        Assert.Equal("message-1", document.Root!.Element(ns + "MessageID")!.Value);
        Assert.Equal("A&B<1>.St", document.Root.Element(ns + "InventoryStatus")!.Element(ns + "SKU")!.Value);
        Assert.Equal("7", document.Root.Element(ns + "InventoryStatus")!.Element(ns + "Quantity")!.Value);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateReviseInventoryStatus_RejectsMoreThanFourLines()
    {
        var items = Enumerable.Range(1, 5)
            .Select(number => new InventoryUpdateItem(number.ToString(), $"SKU-{number}", 0, number))
            .ToArray();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InventoryUpdateRequests.CreateReviseInventoryStatus(items, "message", "1477", "77"));
    }

    [Fact]
    public void DeserializeResponse_ReadsAckCorrelationAndErrors()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ReviseInventoryStatusResponse xmlns="urn:ebay:apis:eBLBaseComponents">
              <Ack>Warning</Ack>
              <CorrelationID>message-1</CorrelationID>
              <Errors>
                <ShortMessage>Hinweis</ShortMessage>
                <LongMessage>Langer Hinweis</LongMessage>
                <ErrorCode>123</ErrorCode>
                <SeverityCode>Warning</SeverityCode>
              </Errors>
              <InventoryStatus><SKU>SKU-1</SKU><ItemID>42</ItemID></InventoryStatus>
            </ReviseInventoryStatusResponse>
            """;

        var response = InventoryUpdateRequests.DeserializeResponse(xml);

        Assert.Equal("Warning", response.Ack);
        Assert.Equal("message-1", response.CorrelationId);
        Assert.Equal("123", Assert.Single(response.Errors).ErrorCode);
        Assert.Equal("SKU-1", Assert.Single(response.InventoryStatuses).Sku);
    }
}
