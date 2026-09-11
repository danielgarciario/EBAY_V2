# Importación idempotente de artículos

## Objetivo

Guardar en SQL Server el resultado del `LMS_ACTIVE_INVENTORY_REPORT` desde el módulo C# que se implementará más adelante.

La importación debe poder ejecutarse más de una vez con el mismo ZIP o el mismo `TaskId` sin duplicar datos.

## Patrón elegido

El patrón elegido es:

1. C# descarga el ZIP original desde eBay.
2. C# calcula el SHA-256 del ZIP.
3. C# extrae y parsea el XML del ZIP.
4. C# crea `DataTable` con la misma forma que los table-valued parameters de SQL Server.
5. C# llama a un stored procedure.
6. SQL Server persiste los datos con `MERGE`.

Este patrón encaja con la forma habitual de trabajar en este entorno: User Defined Table Types para representar los DataTables enviados desde C# y stored procedures para guardar los datos de forma idempotente.

## Scripts implicados

Orden relevante:

1. `Database/DDL/001_CreateSchema.sql`
2. `Database/DDL/002_CreateTypes.sql`
3. `Database/DDL/003_CreateInventoryTables.sql`
4. `Database/DDL/004_CreateInventoryImportTableTypes.sql`
5. `Database/DDL/005_CreateInventoryImportProcedures.sql`
6. `Database/DDL/006_CreateInventoryViews.sql`
7. `Database/DDL/007_CreateMaintenanceProcedures.sql`
8. `Database/DDL/008_CreateSqlAgentJobs.sql`

## Table-valued parameters

### InventoryListingSnapshotImportTableType

Representa las filas `SKUDetails` del XML.

Campos:

1. `ListingRowNumber`
2. `EbayItemId`
3. `ParentSku`
4. `ReportedParentQuantity`
5. `HasVariations`
6. `ParentPriceAmount`
7. `ParentPriceCurrency`

`ListingRowNumber` es una clave temporal creada por C# para enlazar sellables con su listing padre durante la llamada al stored procedure.

### InventorySellableSnapshotImportTableType

Representa unidades vendibles normalizadas.

Para un listing simple, hay una fila vendible que coincide con el `SKUDetails`.

Para un listing con variaciones, hay una fila vendible por cada `Variation`.

Campos:

1. `SellableRowNumber`
2. `ListingRowNumber`
3. `EbayItemId`
4. `ParentSku`
5. `SellableSku`
6. `IsVariation`
7. `ReportedQuantity`
8. `PriceAmount`
9. `PriceCurrency`

`SellableRowNumber` es una clave temporal creada por C# para enlazar características de variación con su unidad vendible durante la llamada al stored procedure.

### InventoryVariationSpecificSnapshotImportTableType

Representa los `NameValueList` de cada variante.

Campos:

1. `SellableRowNumber`
2. `SortOrder`
3. `Name`
4. `Value`

### InventorySellableEfaValidationImportTableType

Representa el resultado de validar una unidad vendible contra EfA.

Campos:

1. `EbayItemId`
2. `SellableSku`
3. `ParsedEfaItem`
4. `ParsedEfaSalesUnit`
5. `ValidationStatus`
6. `EfaItemFound`
7. `EfaSalesUnitFound`
8. `EfaItemIsActive`
9. `Message`

### ArticleCompatibilityIssueImportTableType

Representa incidencias detectadas al comparar eBay contra EfA.

Campos:

1. `EbayItemId`
2. `SellableSku`
3. `ParsedEfaItem`
4. `ParsedEfaSalesUnit`
5. `IssueType`
6. `Severity`
7. `Message`

## Stored procedures

### ImportInventoryReport

Importa el ZIP original y los snapshots normalizados del inventario.

Parámetros principales:

1. Metadatos del feed: `Source`, `FeedType`, `TaskId`, `EbayAck`.
2. ZIP original: `SourceFileName`, `SourceContent`, `SourceFileSha256`.
3. Fechas locales: `StartedAtLocal`, `CompletedAtLocal`, `ImportedAtLocal`.
4. TVPs: `Listings`, `Sellables`, `VariationSpecifics`.
5. Salida: `ImportBatchId`.

Regla de idempotencia:

1. Si existe un batch con el mismo `SourceFileSha256`, se actualiza ese batch.
2. Si existe un batch con el mismo `TaskId`, se actualiza ese batch.
3. Si `TaskId` y `SourceFileSha256` apuntan a batches distintos, se lanza error.
4. Si no existe batch, se crea uno nuevo.

Después, el stored procedure hace `MERGE` de:

1. Listings.
2. Sellables.
3. Variation specifics.

También crea una fila `Pending` en `InventorySellableEfaValidation` para cada unidad vendible nueva.

### UpsertInventorySellableEfaValidation

Actualiza el resultado de validar las unidades vendibles contra EfA.

No crea mapeos manuales. Solo guarda el resultado de separar `SellableSku` en `item + qtun` y comprobar esa combinación en EfA.

### ReplaceArticleCompatibilityIssues

Reemplaza las incidencias abiertas de un batch concreto.

Si una incidencia ya existe abierta para `ImportBatchId + EbayItemId + SellableSku + IssueType`, se actualiza.

Si una incidencia abierta de ese batch ya no viene en el TVP, se marca como resuelta con hora local.

## Hora local

Las columnas de fecha usan hora local Europe/Berlin.

Se evita el sufijo `Utc` en nombres de columnas y parámetros.

Los defaults SQL usan `SYSDATETIME()`.

## Propuesta de clases C#

Estas clases son documentación de diseño. No se han añadido todavía al código de la solución.

La idea es separar cuatro capas:

1. DTOs para deserializar el XML de eBay.
2. Modelo normalizado en memoria.
3. Filas C# que representan los table-valued parameters.
4. Un comando de importación que agrupa los metadatos, el ZIP original y los DataTables.

### DTOs para el XML de eBay

Estos DTOs están pensados para `XmlSerializer`.

Puntos importantes:

1. El namespace `urn:ebay:apis:eBLBaseComponents` debe estar declarado.
2. `ItemID` se trata como `string`.
3. Los precios se tratan como `decimal`.
4. `Price` es opcional en `SKUDetails`, porque los listings con variaciones no tienen precio en el padre.
5. Las listas se inicializan vacías para facilitar la normalización posterior.

```csharp
using System.Xml.Serialization;

namespace EbayV2.Articles.InventoryImport;

internal static class EbayXmlNamespaces
{
    public const string EblBaseComponents = "urn:ebay:apis:eBLBaseComponents";
}

[XmlRoot("BulkDataExchangeResponses", Namespace = EbayXmlNamespaces.EblBaseComponents)]
public sealed class BulkDataExchangeResponsesDto
{
    [XmlElement("ActiveInventoryReport", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public ActiveInventoryReportDto? ActiveInventoryReport { get; set; }
}

public sealed class ActiveInventoryReportDto
{
    [XmlElement("SKUDetails", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public List<SkuDetailsDto> SkuDetails { get; set; } = [];

    [XmlElement("Ack", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Ack { get; set; } = string.Empty;
}

public sealed class SkuDetailsDto
{
    [XmlElement("SKU", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Sku { get; set; } = string.Empty;

    [XmlElement("Quantity", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public int Quantity { get; set; }

    [XmlElement("ItemID", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string ItemId { get; set; } = string.Empty;

    [XmlElement("Price", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public EbayPriceDto? Price { get; set; }

    [XmlArray("Variations", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    [XmlArrayItem("Variation", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public List<VariationDto> Variations { get; set; } = [];
}

public sealed class VariationDto
{
    [XmlElement("SKU", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Sku { get; set; } = string.Empty;

    [XmlElement("Price", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public EbayPriceDto Price { get; set; } = new();

    [XmlElement("Quantity", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public int Quantity { get; set; }

    [XmlArray("VariationSpecifics", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    [XmlArrayItem("NameValueList", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public List<NameValueListDto> VariationSpecifics { get; set; } = [];
}

public sealed class EbayPriceDto
{
    [XmlAttribute("currencyID")]
    public string CurrencyId { get; set; } = string.Empty;

    [XmlText]
    public decimal Amount { get; set; }
}

public sealed class NameValueListDto
{
    [XmlElement("Name", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Value", Namespace = EbayXmlNamespaces.EblBaseComponents)]
    public string Value { get; set; } = string.Empty;
}
```

Ejemplo de deserialización:

```csharp
using System.Xml.Serialization;

var serializer = new XmlSerializer(typeof(BulkDataExchangeResponsesDto));

using var reader = new StringReader(xml);
var response = (BulkDataExchangeResponsesDto)serializer.Deserialize(reader)!;
```

### Modelo normalizado en memoria

Después de deserializar el XML, conviene convertirlo a un modelo interno. Este modelo ya no refleja la estructura XML, sino lo que queremos importar.

```csharp
namespace EbayV2.Articles.InventoryImport;

public sealed record InventoryImportModel(
    string Source,
    string FeedType,
    string? TaskId,
    string EbayAck,
    string SourceFileName,
    byte[] SourceContent,
    string SourceFileSha256,
    DateTime? StartedAtLocal,
    DateTime? CompletedAtLocal,
    DateTime? ImportedAtLocal,
    IReadOnlyList<InventoryListingModel> Listings);

public sealed record InventoryListingModel(
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    int ReportedParentQuantity,
    bool HasVariations,
    decimal? ParentPriceAmount,
    string? ParentPriceCurrency,
    IReadOnlyList<InventorySellableModel> Sellables);

public sealed record InventorySellableModel(
    int SellableRowNumber,
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    string SellableSku,
    bool IsVariation,
    int ReportedQuantity,
    decimal PriceAmount,
    string PriceCurrency,
    IReadOnlyList<VariationSpecificModel> VariationSpecifics);

public sealed record VariationSpecificModel(
    int SellableRowNumber,
    int SortOrder,
    string Name,
    string Value);
```

Regla de normalización:

1. Un `SKUDetails` sin `Variations` genera un `InventoryListingModel` y un `InventorySellableModel`.
2. Un `SKUDetails` con `Variations` genera un `InventoryListingModel` y un `InventorySellableModel` por cada `Variation`.
3. `ListingRowNumber` y `SellableRowNumber` son claves temporales solo para la llamada SQL.
4. `SourceContent` contiene el ZIP original descargado desde eBay, no el XML extraído.

### Filas para table-valued parameters

Estas clases representan exactamente la forma de los User Defined Table Types de SQL Server.

```csharp
namespace EbayV2.Articles.InventoryImport;

public sealed record InventoryListingSnapshotImportRow(
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    int ReportedParentQuantity,
    bool HasVariations,
    decimal? ParentPriceAmount,
    string? ParentPriceCurrency);

public sealed record InventorySellableSnapshotImportRow(
    int SellableRowNumber,
    int ListingRowNumber,
    string EbayItemId,
    string ParentSku,
    string SellableSku,
    bool IsVariation,
    int ReportedQuantity,
    decimal PriceAmount,
    string PriceCurrency);

public sealed record InventoryVariationSpecificSnapshotImportRow(
    int SellableRowNumber,
    int SortOrder,
    string Name,
    string Value);

public sealed record InventorySellableEfaValidationImportRow(
    string EbayItemId,
    string SellableSku,
    string? ParsedEfaItem,
    string? ParsedEfaSalesUnit,
    string ValidationStatus,
    bool? EfaItemFound,
    bool? EfaSalesUnitFound,
    bool? EfaItemIsActive,
    string? Message);

public sealed record ArticleCompatibilityIssueImportRow(
    string EbayItemId,
    string SellableSku,
    string? ParsedEfaItem,
    string? ParsedEfaSalesUnit,
    string IssueType,
    string Severity,
    string Message);
```

### Comando de importación

Esta clase representa la llamada completa a `[ebay].[ImportInventoryReport]`.

```csharp
using System.Data;

namespace EbayV2.Articles.InventoryImport;

public sealed class ImportInventoryReportCommand
{
    public string Source { get; init; } = "SellFeedApi";

    public string FeedType { get; init; } = "LMS_ACTIVE_INVENTORY_REPORT";

    public string? TaskId { get; init; }

    public string EbayAck { get; init; } = "Success";

    public required string SourceFileName { get; init; }

    public required byte[] SourceContent { get; init; }

    public required string SourceFileSha256 { get; init; }

    public DateTime? StartedAtLocal { get; init; }

    public DateTime? CompletedAtLocal { get; init; }

    public DateTime? ImportedAtLocal { get; init; }

    public required DataTable Listings { get; init; }

    public required DataTable Sellables { get; init; }

    public required DataTable VariationSpecifics { get; init; }
}
```

### Builder de DataTables

El builder debe crear columnas con los mismos nombres y en el mismo orden que los TVPs.

```csharp
using System.Data;

namespace EbayV2.Articles.InventoryImport;

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

    public static DataTable BuildEfaValidations(IEnumerable<InventorySellableEfaValidationImportRow> rows)
    {
        var table = new DataTable();

        table.Columns.Add("EbayItemId", typeof(string));
        table.Columns.Add("SellableSku", typeof(string));
        table.Columns.Add("ParsedEfaItem", typeof(string));
        table.Columns.Add("ParsedEfaSalesUnit", typeof(string));
        table.Columns.Add("ValidationStatus", typeof(string));
        table.Columns.Add("EfaItemFound", typeof(bool));
        table.Columns.Add("EfaSalesUnitFound", typeof(bool));
        table.Columns.Add("EfaItemIsActive", typeof(bool));
        table.Columns.Add("Message", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.EbayItemId,
                row.SellableSku,
                DbValue(row.ParsedEfaItem),
                DbValue(row.ParsedEfaSalesUnit),
                row.ValidationStatus,
                DbValue(row.EfaItemFound),
                DbValue(row.EfaSalesUnitFound),
                DbValue(row.EfaItemIsActive),
                DbValue(row.Message));
        }

        return table;
    }

    public static DataTable BuildCompatibilityIssues(IEnumerable<ArticleCompatibilityIssueImportRow> rows)
    {
        var table = new DataTable();

        table.Columns.Add("EbayItemId", typeof(string));
        table.Columns.Add("SellableSku", typeof(string));
        table.Columns.Add("ParsedEfaItem", typeof(string));
        table.Columns.Add("ParsedEfaSalesUnit", typeof(string));
        table.Columns.Add("IssueType", typeof(string));
        table.Columns.Add("Severity", typeof(string));
        table.Columns.Add("Message", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.EbayItemId,
                row.SellableSku,
                DbValue(row.ParsedEfaItem),
                DbValue(row.ParsedEfaSalesUnit),
                row.IssueType,
                row.Severity,
                row.Message);
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

    private static object DbValue(bool? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }
}
```

### Llamada al stored procedure

La llamada final desde C# usará `SqlDbType.Structured`.

```csharp
using Microsoft.Data.SqlClient;
using System.Data;

namespace EbayV2.Articles.InventoryImport;

public static class InventoryImportSqlCommandFactory
{
    public static SqlCommand CreateImportCommand(SqlConnection connection, ImportInventoryReportCommand import)
    {
        var command = connection.CreateCommand();
        command.CommandText = "[ebay].[ImportInventoryReport]";
        command.CommandType = CommandType.StoredProcedure;

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
```

### Relación con el código futuro

Cuando empecemos a escribir código, estas clases se pueden convertir en archivos reales casi directamente.

Lo que habrá que decidir todavía es:

1. En qué proyecto vivirán: probablemente en una librería nueva o en un módulo de aplicación, no en `TestConsole`.
2. Si usamos `Microsoft.Data.SqlClient` directamente o una pequeña abstracción propia para ejecutar stored procedures.
3. Si el parser del ZIP y XML devuelve directamente `InventoryImportModel` o si hay un servicio separado para normalización.
