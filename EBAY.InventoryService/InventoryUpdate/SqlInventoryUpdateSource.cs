using EBAY.DatabaseConnection;
using EBAY.InventoryService.InventoryUpdate.Data;
using System.Data;
using System.Globalization;

namespace EBAY.InventoryService.InventoryUpdate;

internal sealed class SqlInventoryUpdateSource(IEbayDatabaseConnectionFactory connectionFactory)
    : IInventoryUpdateSource
{
    private const string Query = """
        SELECT [EbayItemId],
               [SellableSku],
               [ReportedQuantity],
               [enventa]
        FROM [EBAY].[ebay].[BestandDifference];
        """;

    public async Task<IReadOnlyList<InventoryUpdateItem>> GetDifferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var items = new List<InventoryUpdateItem>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = Query;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = connectionFactory.CommandTimeOutSeconds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var ebayItemIdOrdinal = reader.GetOrdinal("EbayItemId");
        var sellableSkuOrdinal = reader.GetOrdinal("SellableSku");
        var reportedQuantityOrdinal = reader.GetOrdinal("ReportedQuantity");
        var targetQuantityOrdinal = reader.GetOrdinal("enventa");

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(new InventoryUpdateItem(
                reader.GetString(ebayItemIdOrdinal),
                reader.GetString(sellableSkuOrdinal),
                Convert.ToInt32(reader.GetValue(reportedQuantityOrdinal), CultureInfo.InvariantCulture),
                Convert.ToInt32(reader.GetValue(targetQuantityOrdinal), CultureInfo.InvariantCulture)));
        }

        return items;
    }
}
