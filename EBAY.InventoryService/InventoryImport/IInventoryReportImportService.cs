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
    /// <summary>
    /// Pide un inventario nuevo. Lo carga en la base de datos y lo valida. 
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> LoadInventory(CancellationToken ct);
    Task<bool> LoadInventory(string TaskID, CancellationToken ct);
}
