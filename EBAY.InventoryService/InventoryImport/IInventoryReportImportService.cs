namespace EBAY.InventoryService.InventoryImport;

public interface IInventoryReportImportService
{
    Task<InventoryReportImportResult> ImportXmlAsync(
        Stream xmlStream,
        InventoryReportImportRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryReportImportResult> ImportXmlAsync(
        string xml,
        InventoryReportImportRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> LoadInventory(CancellationToken ct);
    Task<bool> LoadInventory(string TaskID, CancellationToken ct);
}
