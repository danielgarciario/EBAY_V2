using System.Text.Json.Serialization;

namespace EBAY.OrdersService.OrderImport;

internal sealed class GetOrdersResponseDto
{
    public string? Href { get; init; }
    public string? Next { get; init; }
    public int Total { get; init; }
    public int Limit { get; init; }
    public int Offset { get; init; }
    public List<EbayOrderDto> Orders { get; init; } = [];
}

internal sealed class EbayOrderDto
{
    public string? OrderId { get; init; }
    public string? LegacyOrderId { get; init; }
    public string? SalesRecordReference { get; init; }
    public string? SellerId { get; init; }
    public string? OrderFulfillmentStatus { get; init; }
    public string? OrderPaymentStatus { get; init; }
    public DateTimeOffset? CreationDate { get; init; }
    public DateTimeOffset? LastModifiedDate { get; init; }
    public BuyerDto? Buyer { get; init; }
    public PricingSummaryDto? PricingSummary { get; init; }
    public CancelStatusDto? CancelStatus { get; init; }
    public PaymentSummaryDto? PaymentSummary { get; init; }
    public List<FulfillmentStartInstructionDto> FulfillmentStartInstructions { get; init; } = [];
    public List<LineItemDto> LineItems { get; init; } = [];
    public MoneyDto? TotalFeeBasisAmount { get; init; }
    public MoneyDto? TotalMarketplaceFee { get; init; }
}

internal sealed class BuyerDto
{
    public string? Username { get; init; }
    public AddressDto? TaxAddress { get; init; }
    public BuyerRegistrationAddressDto? BuyerRegistrationAddress { get; init; }
}

internal sealed class BuyerRegistrationAddressDto
{
    public string? FullName { get; init; }
    public AddressDto? ContactAddress { get; init; }
    public PhoneDto? PrimaryPhone { get; init; }
    public PhoneDto? SecondaryPhone { get; init; }
    public string? Email { get; init; }
}

internal sealed class FulfillmentStartInstructionDto
{
    public string? FulfillmentInstructionsType { get; init; }
    public DateTimeOffset? MinEstimatedDeliveryDate { get; init; }
    public DateTimeOffset? MaxEstimatedDeliveryDate { get; init; }
    public bool? EbaySupportedFulfillment { get; init; }
    public ShippingStepDto? ShippingStep { get; init; }
}

internal sealed class ShippingStepDto
{
    public ShippingAddressDto? ShipTo { get; init; }
    public string? ShippingCarrierCode { get; init; }
    public string? ShippingServiceCode { get; init; }
}

internal sealed class ShippingAddressDto
{
    public string? FullName { get; init; }
    public AddressDto? ContactAddress { get; init; }
    public PhoneDto? PrimaryPhone { get; init; }
    public string? Email { get; init; }
}

internal sealed class AddressDto
{
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateOrProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
}

internal sealed class PhoneDto
{
    public string? PhoneNumber { get; init; }
}

internal sealed class PricingSummaryDto
{
    public MoneyDto? PriceSubtotal { get; init; }
    public MoneyDto? PriceDiscount { get; init; }
    public MoneyDto? DeliveryCost { get; init; }
    public MoneyDto? DeliveryDiscount { get; init; }
    public MoneyDto? Total { get; init; }
}

internal sealed class CancelStatusDto
{
    public string? CancelState { get; init; }
}

internal sealed class PaymentSummaryDto
{
    public List<RefundDto> Refunds { get; init; } = [];
}

internal sealed class RefundDto
{
    public string? RefundId { get; init; }
    public string? RefundReferenceId { get; init; }
    public string? RefundStatus { get; init; }
    public DateTimeOffset? RefundDate { get; init; }
    public MoneyDto? RefundAmount { get; init; }
    public MoneyDto? Amount { get; init; }
}

internal sealed class LineItemDto
{
    public string? LineItemId { get; init; }
    public string? LegacyItemId { get; init; }
    public string? LegacyVariationId { get; init; }
    public string? Sku { get; init; }
    public string? Title { get; init; }
    public int Quantity { get; init; }
    public string? SoldFormat { get; init; }
    public string? ListingMarketplaceId { get; init; }
    public string? PurchaseMarketplaceId { get; init; }
    public string? LineItemFulfillmentStatus { get; init; }
    public MoneyDto? LineItemCost { get; init; }
    public MoneyDto? DiscountedLineItemCost { get; init; }
    public MoneyDto? Total { get; init; }
    public DeliveryCostDto? DeliveryCost { get; init; }
    public List<PromotionDto> AppliedPromotions { get; init; } = [];
    public List<TaxDto> Taxes { get; init; } = [];
    public List<TaxDto> EbayCollectAndRemitTaxes { get; init; } = [];
    public LineItemPropertiesDto? Properties { get; init; }
    public LineItemFulfillmentInstructionsDto? LineItemFulfillmentInstructions { get; init; }
    public ItemLocationDto? ItemLocation { get; init; }
    public List<VariationAspectDto> VariationAspects { get; init; } = [];
}

internal sealed class DeliveryCostDto
{
    public MoneyDto? ShippingCost { get; init; }
    public MoneyDto? DiscountAmount { get; init; }
}

internal sealed class PromotionDto
{
    public MoneyDto? DiscountAmount { get; init; }
    public string? PromotionId { get; init; }
    public string? Description { get; init; }
}

internal sealed class TaxDto
{
    public string? TaxType { get; init; }
    public MoneyDto? Amount { get; init; }
    public string? CollectionMethod { get; init; }
}

internal sealed class LineItemPropertiesDto
{
    public bool? BuyerProtection { get; init; }
}

internal sealed class LineItemFulfillmentInstructionsDto
{
    public DateTimeOffset? MinEstimatedDeliveryDate { get; init; }
    public DateTimeOffset? MaxEstimatedDeliveryDate { get; init; }
    public DateTimeOffset? ShipByDate { get; init; }
    public bool? GuaranteedDelivery { get; init; }
}

internal sealed class ItemLocationDto
{
    public string? Location { get; init; }
    public string? CountryCode { get; init; }
    public string? PostalCode { get; init; }
}

internal sealed class VariationAspectDto
{
    public string? Name { get; init; }
    public string? Value { get; init; }
}

internal sealed class MoneyDto
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Value { get; init; }

    public string? Currency { get; init; }
}
