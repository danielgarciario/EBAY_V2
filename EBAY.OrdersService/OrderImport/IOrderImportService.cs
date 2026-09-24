namespace EBAY.OrdersService.OrderImport;

public interface IOrderImportService
{
    Task<OrderImportRunResult> ImportRecentOrdersAsync(CancellationToken cancellationToken = default);

    Task<OrderImportRunResult> ImportModifiedOrdersAsync(
        DateTime fromLocal,
        CancellationToken cancellationToken = default);
}
