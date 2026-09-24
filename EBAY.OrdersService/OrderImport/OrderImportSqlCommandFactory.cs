using Microsoft.Data.SqlClient;
using System.Data;

namespace EBAY.OrdersService.OrderImport;

internal static class OrderImportSqlCommandFactory
{
    public static SqlCommand CreateImportCommand(
        SqlConnection connection,
        ImportOrdersCommand import,
        int commandTimeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = "[ebay].[ImportOrders]";
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = commandTimeoutSeconds;

        AddNVarChar(command, "@Source", 16, import.Source);
        AddNVarChar(command, "@SourceReference", 2048, import.SourceReference);
        command.Parameters.Add("@SourceContent", SqlDbType.NVarChar, -1).Value = import.SourceContent;
        AddChar(command, "@SourceContentSha256", 64, import.SourceContentSha256);
        AddDateTime2(command, "@StartedAtLocal", import.StartedAtLocal);
        AddDateTime2(command, "@CompletedAtLocal", import.CompletedAtLocal);
        AddDateTime2(command, "@ImportedAtLocal", import.ImportedAtLocal);

        AddStructured(command, "@Orders", "ebay.OrderImportTableType", import.Orders);
        AddStructured(command, "@ShippingAddresses", "ebay.OrderShippingAddressImportTableType", import.ShippingAddresses);
        AddStructured(command, "@LineItems", "ebay.OrderLineItemImportTableType", import.LineItems);
        AddStructured(command, "@VariationAspects", "ebay.OrderLineItemVariationAspectImportTableType", import.VariationAspects);
        AddStructured(command, "@Promotions", "ebay.OrderLineItemPromotionImportTableType", import.Promotions);
        AddStructured(command, "@Taxes", "ebay.OrderLineItemTaxImportTableType", import.Taxes);
        AddStructured(command, "@Refunds", "ebay.OrderRefundImportTableType", import.Refunds);

        var importBatchId = command.Parameters.Add("@ImportBatchId", SqlDbType.BigInt);
        importBatchId.Direction = ParameterDirection.Output;
        return command;
    }

    private static void AddStructured(SqlCommand command, string name, string typeName, DataTable value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Structured);
        parameter.TypeName = typeName;
        parameter.Value = value;
    }

    private static void AddNVarChar(SqlCommand command, string name, int size, string? value)
    {
        command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = value is null ? DBNull.Value : value;
    }

    private static void AddChar(SqlCommand command, string name, int size, string value)
    {
        command.Parameters.Add(name, SqlDbType.Char, size).Value = value;
    }

    private static void AddDateTime2(SqlCommand command, string name, DateTime value)
    {
        command.Parameters.Add(name, SqlDbType.DateTime2).Value = value;
    }
}
