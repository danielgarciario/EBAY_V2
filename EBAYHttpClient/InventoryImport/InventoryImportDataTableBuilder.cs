using System.Data;

namespace EBAYHttpClient.InventoryImport;

public static class InventoryImportDataTableBuilder
{
    public static DataTable BuildListings(IEnumerable<InventoryListingSnapshotImportRow> rows)
    {
        var table = new DataTable();

        table.Columns.Add("ListingRowNumber", typeof(int));
        table.Columns.Add("EbayItemId", typeof(string));
        table.Columns.Add("ParentSku", typeof(string));
        table.Columns.Add("ReportedParentQuantity", typeof(int));
        table.Columns.Add("HasVariations", typeof(bool));
        table.Columns.Add("ParentPriceAmount", typeof(decimal));
        table.Columns.Add("ParentPriceCurrency", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.ListingRowNumber,
                row.EbayItemId,
                row.ParentSku,
                row.ReportedParentQuantity,
                row.HasVariations,
                DbValue(row.ParentPriceAmount),
                DbValue(row.ParentPriceCurrency));
        }

        return table;
    }

    public static DataTable BuildSellables(IEnumerable<InventorySellableSnapshotImportRow> rows)
    {
        var table = new DataTable();

        table.Columns.Add("SellableRowNumber", typeof(int));
        table.Columns.Add("ListingRowNumber", typeof(int));
        table.Columns.Add("EbayItemId", typeof(string));
        table.Columns.Add("ParentSku", typeof(string));
        table.Columns.Add("SellableSku", typeof(string));
        table.Columns.Add("IsVariation", typeof(bool));
        table.Columns.Add("ReportedQuantity", typeof(int));
        table.Columns.Add("PriceAmount", typeof(decimal));
        table.Columns.Add("PriceCurrency", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.SellableRowNumber,
                row.ListingRowNumber,
                row.EbayItemId,
                row.ParentSku,
                row.SellableSku,
                row.IsVariation,
                row.ReportedQuantity,
                row.PriceAmount,
                row.PriceCurrency);
        }

        return table;
    }

    public static DataTable BuildVariationSpecifics(IEnumerable<InventoryVariationSpecificSnapshotImportRow> rows)
    {
        var table = new DataTable();

        table.Columns.Add("SellableRowNumber", typeof(int));
        table.Columns.Add("SortOrder", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Value", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.SellableRowNumber,
                row.SortOrder,
                row.Name,
                row.Value);
        }

        return table;
    }

    private static object DbValue(string? value)
    {
        return value is null ? DBNull.Value : value;
    }

    private static object DbValue(decimal? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }
}
