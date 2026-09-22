namespace EBAY.InventoryService.InventoryUpdate.Data;

public sealed record InventoryUpdateItem(
    string EbayItemId,
    string SellableSku,
    int ReportedQuantity,
    int TargetQuantity);

public enum InventoryUpdateRunStatus
{
    NoChanges,
    Completed,
    CompletedWithWarnings,
    PartiallyFailed,
    Failed,
    AlreadyRunning
}

public enum InventoryUpdateBatchStatus
{
    Success,
    Warning,
    PartialFailure,
    Failure,
    Uncertain
}

public sealed record InventoryUpdateBatchResult(
    string MessageId,
    string? CorrelationId,
    InventoryUpdateBatchStatus Status,
    int RequestedLines,
    int SuccessfulLines,
    int WarningLines,
    int FailedLines,
    int UncertainLines,
    string? Ack,
    int? HttpStatusCode,
    string? ErrorMessage,
    IReadOnlyList<EbayInventoryUpdateError> EbayErrors);

public sealed record InventoryUpdateResult(
    InventoryUpdateRunStatus Status,
    int SourceLines,
    int ValidLines,
    int InvalidLines,
    int SubmittedLines,
    int SuccessfulLines,
    int WarningLines,
    int FailedLines,
    int UncertainLines,
    IReadOnlyList<InventoryUpdateBatchResult> Batches,
    string? ErrorMessage = null);
