using EBAYHttpClient.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace EBAYHttpClient.InventoryImport;

public sealed class InventoryReportImportService(
    IOptions<EBAYDB> options,
    InventoryReportParser parser) : IInventoryReportImportService
{
    private readonly EBAYDB options = options.Value;

    public async Task<InventoryReportImportResult> ImportXmlAsync(
        string xml,
        InventoryReportImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        ArgumentNullException.ThrowIfNull(request);

        var xmlBytes = Encoding.UTF8.GetBytes(xml);
        await using var stream = new MemoryStream(xmlBytes, writable: false);

        return await ImportXmlCoreAsync(stream, xmlBytes, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InventoryReportImportResult> ImportXmlAsync(
        Stream xmlStream,
        InventoryReportImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);
        ArgumentNullException.ThrowIfNull(request);

        await using var buffer = new MemoryStream();
        await xmlStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var xmlBytes = buffer.ToArray();

        await using var parseStream = new MemoryStream(xmlBytes, writable: false);
        return await ImportXmlCoreAsync(parseStream, xmlBytes, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InventoryReportImportResult> ImportXmlCoreAsync(
        Stream xmlStream,
        byte[] xmlBytes,
        InventoryReportImportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Die EBAYDB-Verbindungszeichenfolge ist nicht konfiguriert.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceFileName))
        {
            throw new InvalidOperationException("Der Quelldateiname ist erforderlich.");
        }

        var sourceContent = request.SourceContent is { Length: > 0 }
            ? request.SourceContent
            : xmlBytes;

        var sourceFileSha256 = string.IsNullOrWhiteSpace(request.SourceFileSha256)
            ? ComputeSha256(sourceContent)
            : request.SourceFileSha256.Trim();

        var parsed = parser.Parse(xmlStream);
        var import = BuildImportCommand(request, parsed, sourceContent, sourceFileSha256);

        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = InventoryImportSqlCommandFactory.CreateImportCommand(
            connection,
            import,
            options.CommandTimeoutSeconds);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Der Import hat kein Ergebnis zurückgegeben.");
        }

        return new InventoryReportImportResult(
            reader.GetInt64(reader.GetOrdinal("ImportBatchId")),
            reader.GetInt32(reader.GetOrdinal("SkuDetailsCount")),
            reader.GetInt32(reader.GetOrdinal("VariationCount")),
            reader.GetInt32(reader.GetOrdinal("SellableCount")),
            reader.GetInt32(reader.GetOrdinal("VariationSpecificCount")));
    }

    private ImportInventoryReportCommand BuildImportCommand(
        InventoryReportImportRequest request,
        InventoryReportParseResult parsed,
        byte[] sourceContent,
        string sourceFileSha256)
    {
        var listingRows = new List<InventoryListingSnapshotImportRow>(parsed.Listings.Count);
        var sellableRows = new List<InventorySellableSnapshotImportRow>();
        var variationSpecificRows = new List<InventoryVariationSpecificSnapshotImportRow>();

        foreach (var listing in parsed.Listings)
        {
            listingRows.Add(new InventoryListingSnapshotImportRow(
                listing.ListingRowNumber,
                listing.EbayItemId,
                listing.ParentSku,
                listing.ReportedParentQuantity,
                listing.HasVariations,
                listing.ParentPriceAmount,
                listing.ParentPriceCurrency));

            foreach (var sellable in listing.Sellables)
            {
                sellableRows.Add(new InventorySellableSnapshotImportRow(
                    sellable.SellableRowNumber,
                    sellable.ListingRowNumber,
                    sellable.EbayItemId,
                    sellable.ParentSku,
                    sellable.SellableSku,
                    sellable.IsVariation,
                    sellable.ReportedQuantity,
                    sellable.PriceAmount,
                    sellable.PriceCurrency));

                foreach (var variationSpecific in sellable.VariationSpecifics)
                {
                    variationSpecificRows.Add(new InventoryVariationSpecificSnapshotImportRow(
                        variationSpecific.SellableRowNumber,
                        variationSpecific.SortOrder,
                        variationSpecific.Name,
                        variationSpecific.Value));
                }
            }
        }

        return new ImportInventoryReportCommand
        {
            Source = NormalizeOptional(request.Source) ?? options.DefaultSource,
            FeedType = NormalizeOptional(request.FeedType) ?? options.DefaultFeedType,
            TaskId = NormalizeOptional(request.TaskId),
            EbayAck = parsed.EbayAck,
            SourceFileName = request.SourceFileName.Trim(),
            SourceContent = sourceContent,
            SourceFileSha256 = sourceFileSha256,
            StartedAtLocal = request.StartedAtLocal,
            CompletedAtLocal = request.CompletedAtLocal,
            ImportedAtLocal = request.ImportedAtLocal,
            Listings = InventoryImportDataTableBuilder.BuildListings(listingRows),
            Sellables = InventoryImportDataTableBuilder.BuildSellables(sellableRows),
            VariationSpecifics = InventoryImportDataTableBuilder.BuildVariationSpecifics(variationSpecificRows)
        };
    }

    private static string ComputeSha256(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
