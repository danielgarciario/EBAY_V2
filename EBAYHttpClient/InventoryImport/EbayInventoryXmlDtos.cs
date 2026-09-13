using System.Xml.Serialization;

namespace EBAYHttpClient.InventoryImport;

internal static class EbayXmlNamespaces
{
    public const string EblBaseComponents = "urn:ebay:apis:eBLBaseComponents";
}

[XmlRoot("BulkDataExchangeResponses", Namespace = EbayXmlNamespaces.EblBaseComponents)]
public sealed class BulkDataExchangeResponsesDto
{
    [XmlElement("ActiveInventoryReport", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public ActiveInventoryReportDto? ActiveInventoryReport { get; set; }
}

public sealed class ActiveInventoryReportDto
{
    [XmlElement("SKUDetails", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public List<SkuDetailsDto> SkuDetails { get; set; } = [];

    [XmlElement("Ack", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Ack { get; set; } = string.Empty;
}

public sealed class SkuDetailsDto
{
    [XmlElement("SKU", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Sku { get; set; } = string.Empty;

    [XmlElement("Quantity", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public int Quantity { get; set; }

    [XmlElement("ItemID", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string ItemId { get; set; } = string.Empty;

    [XmlElement("Price", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public EbayPriceDto? Price { get; set; }

    [XmlArray("Variations", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    [XmlArrayItem("Variation", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public List<VariationDto> Variations { get; set; } = [];
}

public sealed class VariationDto
{
    [XmlElement("SKU", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Sku { get; set; } = string.Empty;

    [XmlElement("Price", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public EbayPriceDto? Price { get; set; }

    [XmlElement("Quantity", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public int Quantity { get; set; }

    [XmlArray("VariationSpecifics", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    [XmlArrayItem("NameValueList", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public List<NameValueListDto> VariationSpecifics { get; set; } = [];
}

public sealed class EbayPriceDto
{
    [XmlAttribute("currencyID")]
    public string CurrencyId { get; set; } = string.Empty;

    [XmlText]
    public decimal Amount { get; set; }
}

public sealed class NameValueListDto
{
    [XmlElement("Name", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Value", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Value { get; set; } = string.Empty;
}
