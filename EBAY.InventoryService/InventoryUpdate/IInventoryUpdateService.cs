using EBAY.InventoryService.InventoryUpdate.Data;

namespace EBAY.InventoryService.InventoryUpdate;

public interface IInventoryUpdateService
{
    Task<InventoryUpdateResult> UpdateInventoryAsync(
        CancellationToken cancellationToken = default);
}
