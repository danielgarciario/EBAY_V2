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
}
