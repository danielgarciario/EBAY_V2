using System.Xml.Serialization;

namespace EBAYHttpClient.InventoryImport;

public sealed class InventoryReportParser
{
    private static readonly XmlSerializer Serializer = new(typeof(BulkDataExchangeResponsesDto));

    public InventoryReportParseResult Parse(Stream xmlStream)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);

        var response = (BulkDataExchangeResponsesDto?)Serializer.Deserialize(xmlStream);
        var report = response?.ActiveInventoryReport;

        if (report is null)
        {
            throw new InvalidDataException("Der eBay-Inventarbericht enthält keinen ActiveInventoryReport.");
        }

        var listings = new List<InventoryListingModel>(report.SkuDetails.Count);
        var sellableRowNumber = 0;

        for (var listingIndex = 0; listingIndex < report.SkuDetails.Count; listingIndex++)
        {
            var listingRowNumber = listingIndex + 1;
            var skuDetails = report.SkuDetails[listingIndex];
            var parentSku = Required(skuDetails.Sku, $"SKUDetails[{listingRowNumber}].SKU");
            var ebayItemId = Required(skuDetails.ItemId, $"SKUDetails[{listingRowNumber}].ItemID");
            var variations = skuDetails.Variations;
            var hasVariations = variations.Count > 0;
            var sellables = new List<InventorySellableModel>(hasVariations ? variations.Count : 1);

            if (hasVariations)
            {
                for (var variationIndex = 0; variationIndex < variations.Count; variationIndex++)
                {
                    var variation = variations[variationIndex];
                    var price = RequiredPrice(variation.Price, $"SKUDetails[{listingRowNumber}].Variation[{variationIndex + 1}].Price");
                    sellableRowNumber++;

                    sellables.Add(new InventorySellableModel(
                        sellableRowNumber,
                        listingRowNumber,
                        ebayItemId,
                        parentSku,
                        Required(variation.Sku, $"SKUDetails[{listingRowNumber}].Variation[{variationIndex + 1}].SKU"),
                        IsVariation: true,
                        variation.Quantity,
                        price.Amount,
                        Required(price.CurrencyId, $"SKUDetails[{listingRowNumber}].Variation[{variationIndex + 1}].Price.currencyID"),
                        BuildVariationSpecifics(sellableRowNumber, variation.VariationSpecifics)));
                }
            }
            else
            {
                var price = RequiredPrice(skuDetails.Price, $"SKUDetails[{listingRowNumber}].Price");
                sellableRowNumber++;

                sellables.Add(new InventorySellableModel(
                    sellableRowNumber,
                    listingRowNumber,
                    ebayItemId,
                    parentSku,
                    parentSku,
                    IsVariation: false,
                    skuDetails.Quantity,
                    price.Amount,
                    Required(price.CurrencyId, $"SKUDetails[{listingRowNumber}].Price.currencyID"),
                    []));
            }

            listings.Add(new InventoryListingModel(
                listingRowNumber,
                ebayItemId,
                parentSku,
                skuDetails.Quantity,
                hasVariations,
                skuDetails.Price?.Amount,
                NormalizeOptional(skuDetails.Price?.CurrencyId),
                sellables));
        }

        return new InventoryReportParseResult(NormalizeOptional(report.Ack) ?? "Success", listings);
    }

    private static IReadOnlyList<VariationSpecificModel> BuildVariationSpecifics(
        int sellableRowNumber,
        IReadOnlyList<NameValueListDto> nameValueLists)
    {
        var variationSpecifics = new List<VariationSpecificModel>(nameValueLists.Count);

        for (var index = 0; index < nameValueLists.Count; index++)
        {
            var nameValue = nameValueLists[index];
            var sortOrder = index + 1;

            variationSpecifics.Add(new VariationSpecificModel(
                sellableRowNumber,
                sortOrder,
                Required(nameValue.Name, $"VariationSpecifics[{sellableRowNumber}][{sortOrder}].Name"),
                Required(nameValue.Value, $"VariationSpecifics[{sellableRowNumber}][{sortOrder}].Value")));
        }

        return variationSpecifics;
    }

    private static EbayPriceDto RequiredPrice(EbayPriceDto? price, string fieldName)
    {
        return price ?? throw new InvalidDataException($"{fieldName} ist erforderlich.");
    }

    private static string Required(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value);

        return normalized ?? throw new InvalidDataException($"{fieldName} ist erforderlich.");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record InventoryReportParseResult(
    string EbayAck,
    IReadOnlyList<InventoryListingModel> Listings);
