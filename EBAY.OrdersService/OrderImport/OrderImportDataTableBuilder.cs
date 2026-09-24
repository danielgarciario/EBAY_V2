using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EBAY.OrdersService.OrderImport;

internal static class OrderImportDataTableBuilder
{
    private static readonly TimeZoneInfo BerlinTimeZone = GetBerlinTimeZone();

    public static OrderImportTables Build(GetOrdersResponseDto response)
    {
        var orders = CreateOrdersTable();
        var shippingAddresses = CreateShippingAddressesTable();
        var lineItems = CreateLineItemsTable();
        var variationAspects = CreateVariationAspectsTable();
        var promotions = CreatePromotionsTable();
        var taxes = CreateTaxesTable();
        var refunds = CreateRefundsTable();

        var orderRowNumber = 0;
        var lineItemRowNumber = 0;

        foreach (var order in response.Orders)
        {
            orderRowNumber++;
            var shippingInstruction = order.FulfillmentStartInstructions
                .SingleOrDefault(instruction => string.Equals(
                    instruction.FulfillmentInstructionsType,
                    "SHIP_TO",
                    StringComparison.OrdinalIgnoreCase));
            var shippingAddress = shippingInstruction?.ShippingStep?.ShipTo
                ?? throw new InvalidOperationException($"Für die eBay-Bestellung {Required(order.OrderId, "orderId")} fehlt die SHIP_TO-Adresse.");

            var pricing = order.PricingSummary;
            var total = Required(pricing?.Total, $"pricingSummary.total der Bestellung {Required(order.OrderId, "orderId")}");
            var registration = order.Buyer?.BuyerRegistrationAddress;
            var registrationAddress = registration?.ContactAddress;
            var taxAddress = order.Buyer?.TaxAddress;

            orders.Rows.Add(
                orderRowNumber,
                Required(order.OrderId, "orderId"),
                DbValue(order.LegacyOrderId),
                DbValue(order.SalesRecordReference),
                Required(order.SellerId, "sellerId"),
                Required(order.Buyer?.Username, "buyer.username"),
                RequiredLocal(order.CreationDate, "creationDate"),
                RequiredLocal(order.LastModifiedDate, "lastModifiedDate"),
                Required(order.OrderFulfillmentStatus, "orderFulfillmentStatus"),
                Required(order.OrderPaymentStatus, "orderPaymentStatus"),
                Required(order.CancelStatus?.CancelState, "cancelStatus.cancelState"),
                DbValue(pricing?.PriceSubtotal?.Value),
                DbValue(pricing?.PriceSubtotal?.Currency),
                DbValue(pricing?.PriceDiscount?.Value),
                DbValue(pricing?.DeliveryCost?.Value),
                DbValue(pricing?.DeliveryDiscount?.Value),
                total.Value,
                Required(total.Currency, "pricingSummary.total.currency"),
                DbValue(order.TotalFeeBasisAmount?.Value),
                DbValue(order.TotalFeeBasisAmount?.Currency),
                DbValue(order.TotalMarketplaceFee?.Value),
                DbValue(order.TotalMarketplaceFee?.Currency),
                DbValue(taxAddress?.City),
                DbValue(taxAddress?.PostalCode),
                DbValue(taxAddress?.CountryCode),
                DbValue(registration?.FullName),
                DbValue(registrationAddress?.AddressLine1),
                DbValue(registrationAddress?.AddressLine2),
                DbValue(registrationAddress?.City),
                DbValue(registrationAddress?.PostalCode),
                DbValue(registrationAddress?.CountryCode),
                DbValue(registration?.PrimaryPhone?.PhoneNumber),
                DbValue(registration?.SecondaryPhone?.PhoneNumber),
                DbValue(registration?.Email));

            var shippingContactAddress = shippingAddress.ContactAddress;
            shippingAddresses.Rows.Add(
                orderRowNumber,
                //Required(shippingAddress.FullName, "shippingStep.shipTo.fullName"),
                DbValue(shippingAddress?.FullName),
                //Required(shippingContactAddress?.AddressLine1, "shippingStep.shipTo.contactAddress.addressLine1"),
                DbValue(shippingContactAddress?.AddressLine1),
                DbValue(shippingContactAddress?.AddressLine2),
                //Required(shippingContactAddress?.City, "shippingStep.shipTo.contactAddress.city"),
                DbValue(shippingContactAddress?.City),
                DbValue(shippingContactAddress?.StateOrProvince),

                //Required(shippingContactAddress?.PostalCode, "shippingStep.shipTo.contactAddress.postalCode"),
                //Required(shippingContactAddress?.CountryCode, "shippingStep.shipTo.contactAddress.countryCode"),
                DbValue(shippingContactAddress?.PostalCode),
                DbValue(shippingContactAddress?.CountryCode),
                DbValue(shippingAddress.PrimaryPhone?.PhoneNumber),
                DbValue(shippingAddress.Email),
                DbValue(shippingInstruction?.ShippingStep?.ShippingCarrierCode),
                DbValue(shippingInstruction?.ShippingStep?.ShippingServiceCode),
                DbValue(ToLocal(shippingInstruction?.MinEstimatedDeliveryDate)),
                DbValue(ToLocal(shippingInstruction?.MaxEstimatedDeliveryDate)),
                DbValue(shippingInstruction?.EbaySupportedFulfillment));

            foreach (var lineItem in order.LineItems)
            {
                lineItemRowNumber++;
                var lineTotal = Required(lineItem.Total, $"lineItems.total der Bestellung {order.OrderId}");
                var delivery = lineItem.DeliveryCost;
                var fulfillment = lineItem.LineItemFulfillmentInstructions;

                lineItems.Rows.Add(
                    lineItemRowNumber,
                    orderRowNumber,
                    Required(lineItem.LineItemId, "lineItems.lineItemId"),
                    DbValue(lineItem.LegacyItemId),
                    DbValue(lineItem.LegacyVariationId),
                    Required(lineItem.Sku, "lineItems.sku"),
                    Required(lineItem.Title, "lineItems.title"),
                    lineItem.Quantity,
                    DbValue(lineItem.SoldFormat),
                    Required(lineItem.ListingMarketplaceId, "lineItems.listingMarketplaceId"),
                    Required(lineItem.PurchaseMarketplaceId, "lineItems.purchaseMarketplaceId"),
                    Required(lineItem.LineItemFulfillmentStatus, "lineItems.lineItemFulfillmentStatus"),
                    DbValue(lineItem.LineItemCost?.Value),
                    DbValue(lineItem.LineItemCost?.Currency),
                    DbValue(lineItem.DiscountedLineItemCost?.Value),
                    DbValue(lineItem.DiscountedLineItemCost?.Currency),
                    lineTotal.Value,
                    Required(lineTotal.Currency, "lineItems.total.currency"),
                    DbValue(delivery?.ShippingCost?.Value),
                    DbValue(delivery?.ShippingCost?.Currency),
                    DbValue(delivery?.DiscountAmount?.Value),
                    DbValue(delivery?.DiscountAmount?.Currency),
                    DbValue(lineItem.Properties?.BuyerProtection),
                    DbValue(ToLocal(fulfillment?.MinEstimatedDeliveryDate)),
                    DbValue(ToLocal(fulfillment?.MaxEstimatedDeliveryDate)),
                    DbValue(ToLocal(fulfillment?.ShipByDate)),
                    DbValue(fulfillment?.GuaranteedDelivery),
                    DbValue(lineItem.ItemLocation?.Location),
                    DbValue(lineItem.ItemLocation?.CountryCode),
                    DbValue(lineItem.ItemLocation?.PostalCode));

                AddVariationAspects(variationAspects, lineItemRowNumber, lineItem.VariationAspects);
                AddPromotions(promotions, lineItemRowNumber, lineItem.AppliedPromotions);
                AddTaxes(taxes, lineItemRowNumber, "Tax", lineItem.Taxes);
                AddTaxes(taxes, lineItemRowNumber, "EbayCollectAndRemitTax", lineItem.EbayCollectAndRemitTaxes);
            }

            foreach (var refund in order.PaymentSummary?.Refunds ?? [])
            {
                var refundAmount = Required(refund.RefundAmount ?? refund.Amount, "paymentSummary.refunds.refundAmount");
                refunds.Rows.Add(
                    orderRowNumber,
                    RefundId(refund),
                    DbValue(refund.RefundReferenceId),
                    Required(refund.RefundStatus, "paymentSummary.refunds.refundStatus"),
                    DbValue(ToLocal(refund.RefundDate)),
                    refundAmount.Value,
                    Required(refundAmount.Currency, "paymentSummary.refunds.refundAmount.currency"));
            }
        }

        return new OrderImportTables(orders, shippingAddresses, lineItems, variationAspects, promotions, taxes, refunds);
    }

    private static void AddVariationAspects(DataTable table, int lineItemRowNumber, IReadOnlyList<VariationAspectDto> aspects)
    {
        for (var index = 0; index < aspects.Count; index++)
        {
            var aspect = aspects[index];
            table.Rows.Add(lineItemRowNumber, index + 1, Required(aspect.Name, "variationAspects.name"), Required(aspect.Value, "variationAspects.value"));
        }
    }

    private static void AddPromotions(DataTable table, int lineItemRowNumber, IReadOnlyList<PromotionDto> sourcePromotions)
    {
        for (var index = 0; index < sourcePromotions.Count; index++)
        {
            var promotion = sourcePromotions[index];
            var amount = Required(promotion.DiscountAmount, "appliedPromotions.discountAmount");
            table.Rows.Add(lineItemRowNumber, index + 1, DbValue(promotion.PromotionId), DbValue(promotion.Description), amount.Value, Required(amount.Currency, "appliedPromotions.discountAmount.currency"));
        }
    }

    private static void AddTaxes(DataTable table, int lineItemRowNumber, string taxKind, IReadOnlyList<TaxDto> sourceTaxes)
    {
        for (var index = 0; index < sourceTaxes.Count; index++)
        {
            var tax = sourceTaxes[index];
            var amount = Required(tax.Amount, "taxes.amount");
            table.Rows.Add(lineItemRowNumber, taxKind, index + 1, Required(tax.TaxType, "taxes.taxType"), amount.Value, Required(amount.Currency, "taxes.amount.currency"), DbValue(tax.CollectionMethod));
        }
    }

    private static string RefundId(RefundDto refund)
    {
        if (!string.IsNullOrWhiteSpace(refund.RefundId))
        {
            return refund.RefundId.Trim();
        }

        var serialized = JsonSerializer.Serialize(refund);
        return $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized))).ToLowerInvariant()}";
    }

    private static DateTime RequiredLocal(DateTimeOffset? value, string fieldName) =>
        ToLocal(value) ?? throw new InvalidOperationException($"Das eBay-Feld {fieldName} ist erforderlich.");

    private static DateTime? ToLocal(DateTimeOffset? value) =>
        value.HasValue ? TimeZoneInfo.ConvertTime(value.Value, BerlinTimeZone).DateTime : null;

    private static T Required<T>(T? value, string fieldName) where T : class =>
        value ?? throw new InvalidOperationException($"Das eBay-Feld {fieldName} ist erforderlich.");

    private static string Required(string? value, string fieldName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Das eBay-Feld {fieldName} ist erforderlich.")
            : value.Trim();

    private static object DbValue(string? value) => value is null ? DBNull.Value : value;

    private static object DbValue(decimal? value) => value.HasValue ? value.Value : DBNull.Value;

    private static object DbValue(DateTime? value) => value.HasValue ? value.Value : DBNull.Value;

    private static object DbValue(bool? value) => value.HasValue ? value.Value : DBNull.Value;

    private static TimeZoneInfo GetBerlinTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
    }

    private static DataTable CreateOrdersTable()
    {
        var table = new DataTable();
        AddColumns(table, "OrderRowNumber:int,EbayOrderId:string,LegacyOrderId:string,SalesRecordReference:string,SellerId:string,BuyerUsername:string,CreationDateLocal:DateTime,LastModifiedDateLocal:DateTime,OrderFulfillmentStatus:string,OrderPaymentStatus:string,CancelState:string,PriceSubtotalAmount:decimal,PriceSubtotalCurrency:string,PriceDiscountAmount:decimal,DeliveryCostAmount:decimal,DeliveryDiscountAmount:decimal,TotalAmount:decimal,TotalCurrency:string,TotalFeeBasisAmount:decimal,TotalFeeBasisCurrency:string,TotalMarketplaceFeeAmount:decimal,TotalMarketplaceFeeCurrency:string,BuyerTaxCity:string,BuyerTaxPostalCode:string,BuyerTaxCountryCode:string,BuyerRegistrationFullName:string,BuyerRegistrationAddressLine1:string,BuyerRegistrationAddressLine2:string,BuyerRegistrationCity:string,BuyerRegistrationPostalCode:string,BuyerRegistrationCountryCode:string,BuyerRegistrationPrimaryPhone:string,BuyerRegistrationSecondaryPhone:string,BuyerRegistrationEmail:string");
        return table;
    }

    private static DataTable CreateShippingAddressesTable()
    {
        var table = new DataTable();
        AddColumns(table, "OrderRowNumber:int,FullName:string,AddressLine1:string,AddressLine2:string,City:string,StateOrProvince:string,PostalCode:string,CountryCode:string,PrimaryPhone:string,Email:string,ShippingCarrierCode:string,ShippingServiceCode:string,MinEstimatedDeliveryDateLocal:DateTime,MaxEstimatedDeliveryDateLocal:DateTime,EbaySupportedFulfillment:bool");
        return table;
    }

    private static DataTable CreateLineItemsTable()
    {
        var table = new DataTable();
        AddColumns(table, "LineItemRowNumber:int,OrderRowNumber:int,EbayLineItemId:string,LegacyItemId:string,LegacyVariationId:string,Sku:string,Title:string,Quantity:int,SoldFormat:string,ListingMarketplaceId:string,PurchaseMarketplaceId:string,LineItemFulfillmentStatus:string,LineItemCostAmount:decimal,LineItemCostCurrency:string,DiscountedLineItemCostAmount:decimal,DiscountedLineItemCostCurrency:string,TotalAmount:decimal,TotalCurrency:string,ShippingCostAmount:decimal,ShippingCostCurrency:string,ShippingDiscountAmount:decimal,ShippingDiscountCurrency:string,BuyerProtection:bool,MinEstimatedDeliveryDateLocal:DateTime,MaxEstimatedDeliveryDateLocal:DateTime,ShipByDateLocal:DateTime,GuaranteedDelivery:bool,ItemLocation:string,ItemLocationCountryCode:string,ItemLocationPostalCode:string");
        return table;
    }

    private static DataTable CreateVariationAspectsTable()
    {
        var table = new DataTable();
        AddColumns(table, "LineItemRowNumber:int,SortOrder:int,Name:string,Value:string");
        return table;
    }

    private static DataTable CreatePromotionsTable()
    {
        var table = new DataTable();
        AddColumns(table, "LineItemRowNumber:int,SortOrder:int,PromotionId:string,Description:string,DiscountAmount:decimal,DiscountCurrency:string");
        return table;
    }

    private static DataTable CreateTaxesTable()
    {
        var table = new DataTable();
        AddColumns(table, "LineItemRowNumber:int,TaxKind:string,SortOrder:int,TaxType:string,Amount:decimal,Currency:string,CollectionMethod:string");
        return table;
    }

    private static DataTable CreateRefundsTable()
    {
        var table = new DataTable();
        AddColumns(table, "OrderRowNumber:int,EbayRefundId:string,RefundReferenceId:string,RefundStatus:string,RefundDateLocal:DateTime,Amount:decimal,Currency:string");
        return table;
    }

    private static void AddColumns(DataTable table, string definition)
    {
        foreach (var column in definition.Split(','))
        {
            var parts = column.Split(':');
            table.Columns.Add(parts[0], parts[1] switch
            {
                "int" => typeof(int),
                "decimal" => typeof(decimal),
                "DateTime" => typeof(DateTime),
                "bool" => typeof(bool),
                _ => typeof(string)
            });
        }
    }
}
