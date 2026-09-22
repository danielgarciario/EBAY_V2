using System.Xml.Serialization;

namespace EBAY.InventoryService.InventoryUpdate.Data;

public static class EbayTradingXmlNamespaces
{
    public const string Components = "urn:ebay:apis:eBLBaseComponents";
}

[XmlRoot("ReviseInventoryStatusRequest", Namespace = EbayTradingXmlNamespaces.Components)]
public sealed class ReviseInventoryStatusRequest
{
    [XmlElement("ErrorLanguage", Order = 1)]
    public string ErrorLanguage { get; set; } = "de_DE";

    [XmlElement("MessageID", Order = 2)]
    public required string MessageId { get; set; }

    [XmlElement("WarningLevel", Order = 3)]
    public string WarningLevel { get; set; } = "Low";

    [XmlElement("InventoryStatus", Order = 4)]
    public List<ReviseInventoryStatusLine> InventoryStatuses { get; set; } = [];
}

public sealed class ReviseInventoryStatusLine
{
    [XmlElement("ItemID", Order = 1)]
    public required string ItemId { get; set; }

    [XmlElement("SKU", Order = 2)]
    public required string Sku { get; set; }

    [XmlElement("Quantity", Order = 3)]
    public int Quantity { get; set; }
}

[XmlRoot("ReviseInventoryStatusResponse", Namespace = EbayTradingXmlNamespaces.Components)]
public sealed class ReviseInventoryStatusResponse
{
    [XmlElement("Timestamp")]
    public DateTime? Timestamp { get; set; }

    [XmlElement("Ack")]
    public string? Ack { get; set; }

    [XmlElement("CorrelationID")]
    public string? CorrelationId { get; set; }

    [XmlElement("Errors")]
    public List<EbayInventoryUpdateError> Errors { get; set; } = [];

    [XmlElement("Version")]
    public string? Version { get; set; }

    [XmlElement("Build")]
    public string? Build { get; set; }

    [XmlElement("InventoryStatus")]
    public List<ReviseInventoryStatusResponseLine> InventoryStatuses { get; set; } = [];
}

public sealed class ReviseInventoryStatusResponseLine
{
    [XmlElement("SKU")]
    public string? Sku { get; set; }

    [XmlElement("ItemID")]
    public string? ItemId { get; set; }
}

public sealed class EbayInventoryUpdateError
{
    [XmlElement("ShortMessage")]
    public string? ShortMessage { get; set; }

    [XmlElement("LongMessage")]
    public string? LongMessage { get; set; }

    [XmlElement("ErrorCode")]
    public string? ErrorCode { get; set; }

    [XmlElement("SeverityCode")]
    public string? SeverityCode { get; set; }

    [XmlElement("ErrorClassification")]
    public string? ErrorClassification { get; set; }

    [XmlElement("ErrorParameters")]
    public List<EbayInventoryUpdateErrorParameter> Parameters { get; set; } = [];
}

public sealed class EbayInventoryUpdateErrorParameter
{
    [XmlAttribute("ParamID")]
    public string? ParameterId { get; set; }

    [XmlElement("Value")]
    public string? Value { get; set; }
}
