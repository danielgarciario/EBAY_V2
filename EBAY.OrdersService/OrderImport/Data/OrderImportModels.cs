using System.Data;

namespace EBAY.OrdersService.OrderImport;

public enum OrderImportRunStatus
{
    Completed,
    NoOrders,
    AlreadyRunning,
    Failed
}

public sealed record OrderImportRunResult(
    OrderImportRunStatus Status,
    DateTime FromLocal,
    DateTime CompletedAtLocal,
    int PagesRead,
    int OrdersReceived,
    int OrdersImportedOrUpdated,
    IReadOnlyList<long> ImportBatchIds,
    string? ErrorMessage = null);

internal sealed class ImportOrdersCommand
{
    public required string Source { get; init; }
    public required string SourceReference { get; init; }
    public required string SourceContent { get; init; }
    public required string SourceContentSha256 { get; init; }
    public required DateTime StartedAtLocal { get; init; }
    public required DateTime CompletedAtLocal { get; init; }
    public required DateTime ImportedAtLocal { get; init; }
    public required DataTable Orders { get; init; }
    public required DataTable ShippingAddresses { get; init; }
    public required DataTable LineItems { get; init; }
    public required DataTable VariationAspects { get; init; }
    public required DataTable Promotions { get; init; }
    public required DataTable Taxes { get; init; }
    public required DataTable Refunds { get; init; }
}

internal sealed record ImportOrdersSqlResult(long ImportBatchId, int ReceivedOrderCount, int ImportedOrUpdatedOrderCount);

internal sealed record OrderImportRow(
    int OrderRowNumber,
    EbayOrderDto Order,
    ShippingAddressDto ShippingAddress);

internal sealed record OrderLineItemImportRow(int LineItemRowNumber, int OrderRowNumber, LineItemDto LineItem);

internal sealed record OrderImportTables(
    DataTable Orders,
    DataTable ShippingAddresses,
    DataTable LineItems,
    DataTable VariationAspects,
    DataTable Promotions,
    DataTable Taxes,
    DataTable Refunds);
