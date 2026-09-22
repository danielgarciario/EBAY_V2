using EBAY.InventoryService.InventoryUpdate.Data;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace EBAY.InventoryService.InventoryUpdate;

public static class InventoryUpdateRequests
{
    private const int MaxLinesPerRequest = 4;

    /// <summary>
    /// Creates a Trading API ReviseInventoryStatus request.
    /// https://developer.ebay.com/devzone/xml/docs/Reference/eBay/ReviseInventoryStatus.html
    /// </summary>
    public static HttpRequestMessage CreateReviseInventoryStatus(
        IReadOnlyCollection<InventoryUpdateItem> items,
        string messageId,
        string apiVersion,
        string siteId)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(siteId);

        if (items.Count is < 1 or > MaxLinesPerRequest)
        {
            throw new ArgumentOutOfRangeException(
                nameof(items),
                $"Eine Anfrage muss zwischen 1 und {MaxLinesPerRequest} Positionen enthalten.");
        }

        var payload = new ReviseInventoryStatusRequest
        {
            MessageId = messageId,
            InventoryStatuses = items
                .Select(item => new ReviseInventoryStatusLine
                {
                    ItemId = item.EbayItemId,
                    Sku = item.SellableSku,
                    Quantity = item.TargetQuantity
                })
                .ToList()
        };

        var request = new HttpRequestMessage(HttpMethod.Post, string.Empty);
        request.Headers.Add("X-EBAY-API-CALL-NAME", "ReviseInventoryStatus");
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", apiVersion);
        request.Headers.Add("X-EBAY-API-SITEID", siteId);
        request.Content = new StringContent(Serialize(payload), Encoding.UTF8, "text/xml");
        return request;
    }

    public static ReviseInventoryStatusResponse DeserializeResponse(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        var serializer = new XmlSerializer(typeof(ReviseInventoryStatusResponse));
        using var reader = new StringReader(xml);
        return serializer.Deserialize(reader) as ReviseInventoryStatusResponse
            ?? throw new InvalidOperationException("Die eBay-Antwort konnte nicht gelesen werden.");
    }

    private static string Serialize(ReviseInventoryStatusRequest payload)
    {
        var serializer = new XmlSerializer(typeof(ReviseInventoryStatusRequest));
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add(string.Empty, EbayTradingXmlNamespaces.Components);

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = false,
            OmitXmlDeclaration = false
        };

        using var writer = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(writer, settings))
        {
            serializer.Serialize(xmlWriter, payload, namespaces);
        }

        return writer.ToString();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
