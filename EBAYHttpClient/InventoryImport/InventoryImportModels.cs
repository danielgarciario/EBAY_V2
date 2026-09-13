using System.Data;

namespace EBAYHttpClient.InventoryImport;

public sealed record InventoryListingModel(
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    int ReportedParentQuantity,
    bool HasVariations,
    decimal? ParentPriceAmount,
    string? ParentPriceCurrency,
    IReadOnlyList<InventorySellableModel> Sellables);

public sealed record InventorySellableModel(
    int SellableRowNumber,
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    string SellableSku,
    bool IsVariation,
    int ReportedQuantity,
    decimal PriceAmount,
    string PriceCurrency,
    IReadOnlyList<VariationSpecificModel> VariationSpecifics);

public sealed record VariationSpecificModel(
    int SellableRowNumber,
    int SortOrder,
    string Name,
    string Value);

public sealed record InventoryListingSnapshotImportRow(
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    int ReportedParentQuantity,
    bool HasVariations,
    decimal? ParentPriceAmount,
    string? ParentPriceCurrency);

public sealed record InventorySellableSnapshotImportRow(
    int SellableRowNumber,
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    string SellableSku,
    bool IsVariation,
    int ReportedQuantity,
    decimal PriceAmount,
    string PriceCurrency);

public sealed record InventoryVariationSpecificSnapshotImportRow(
    int SellableRowNumber,
    int SortOrder,
    string Name,
    string Value);

public sealed class ImportInventoryReportCommand
{
    public string Source { get; init; } = "SellFeedApi";

    public string FeedType { get; init; } = "LMS_ACTIVE_INVENTORY_REPORT";

    public string? TaskId { get; init; }

    public string EbayAck { get; init; } = "Success";

    public required string SourceFileName { get; init; }

    public required byte[] SourceContent { get; init; }

    public required string SourceFileSha256 { get; init; }

    public DateTime? StartedAtLocal { get; init; }

    public DateTime? CompletedAtLocal { get; init; }

    public DateTime? ImportedAtLocal { get; init; }

    public required DataTable Listings { get; init; }

    public required DataTable Sellables { get; init; }

    public required DataTable VariationSpecifics { get; init; }
}

public sealed class InventoryReportImportRequest
{
    public string? Source { get; init; }

    public string? FeedType { get; init; }

    public string? TaskId { get; init; }

    public required string SourceFileName { get; init; }

    public byte[]? SourceContent { get; init; }

    public string? SourceFileSha256 { get; init; }

    public DateTime? StartedAtLocal { get; init; }

    public DateTime? CompletedAtLocal { get; init; }

    public DateTime? ImportedAtLocal { get; init; }
}

public sealed record InventoryReportImportResult(
    long ImportBatchId,
    int SkuDetailsCount,
    int VariationCount,
    int SellableCount,
    int VariationSpecificCount);
