using Microsoft.Data.SqlClient;
using System.Data;

namespace EBAY.InventoryService.InventoryImport;

public static class InventoryImportSqlCommandFactory
{
    public static SqlCommand CreateImportCommand(SqlConnection connection, ImportInventoryReportCommand import, int commandTimeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = "[ebay].[ImportInventoryReport]";
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = commandTimeoutSeconds;

        AddNVarChar(command, "@Source", 64, import.Source);
        AddNVarChar(command, "@FeedType", 64, import.FeedType);
        AddNVarChar(command, "@TaskId", 128, import.TaskId);
        AddNVarChar(command, "@EbayAck", 32, import.EbayAck);
        AddNVarChar(command, "@SourceFileName", 260, import.SourceFileName);
        command.Parameters.Add("@SourceContent", SqlDbType.VarBinary, -1).Value = import.SourceContent;
        AddChar(command, "@SourceFileSha256", 64, import.SourceFileSha256);
        AddDateTime2(command, "@StartedAtLocal", import.StartedAtLocal);
        AddDateTime2(command, "@CompletedAtLocal", import.CompletedAtLocal);
        AddDateTime2(command, "@ImportedAtLocal", import.ImportedAtLocal);

        AddStructured(command, "@Listings", "ebay.InventoryListingSnapshotImportTableType", import.Listings);
        AddStructured(command, "@Sellables", "ebay.InventorySellableSnapshotImportTableType", import.Sellables);
        AddStructured(command, "@VariationSpecifics", "ebay.InventoryVariationSpecificSnapshotImportTableType", import.VariationSpecifics);

        var importBatchId = command.Parameters.Add("@ImportBatchId", SqlDbType.BigInt);
        importBatchId.Direction = ParameterDirection.Output;

        return command;
    }

    public static SqlCommand CheckedImportAgainstEFACommand(SqlConnection connection, int commandTimeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = "[ebay].[CheckedImportAgainstEFA]";
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = commandTimeoutSeconds;

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
        command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = DbValue(value);
    }

    private static void AddChar(SqlCommand command, string name, int size, string value)
    {
        command.Parameters.Add(name, SqlDbType.Char, size).Value = value;
    }

    private static void AddDateTime2(SqlCommand command, string name, DateTime? value)
    {
        command.Parameters.Add(name, SqlDbType.DateTime2).Value = DbValue(value);
    }

    private static object DbValue(string? value)
    {
        return value is null ? DBNull.Value : value;
    }

    private static object DbValue(DateTime? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }
}
