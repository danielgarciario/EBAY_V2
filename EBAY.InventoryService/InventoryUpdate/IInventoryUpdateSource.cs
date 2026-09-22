using EBAY.InventoryService.InventoryUpdate.Data;

namespace EBAY.InventoryService.InventoryUpdate;

public interface IInventoryUpdateSource
{
    Task<IReadOnlyList<InventoryUpdateItem>> GetDifferencesAsync(
        CancellationToken cancellationToken = default);
}
