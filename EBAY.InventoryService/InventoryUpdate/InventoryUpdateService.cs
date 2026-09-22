using EBAY.InventoryService.InventoryUpdate.Data;
using EBAYHttpClient;
using EBAYHttpClient.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace EBAY.InventoryService.InventoryUpdate;

public sealed class InventoryUpdateService(
    ILogger<InventoryUpdateService> log,
    IInventoryUpdateSource source,
    IEBayHttpClientFactory httpClientFactory,
    IOptions<EBAYClientOptions> options,
    InventoryUpdateExecutionLock executionLock) : IInventoryUpdateService
{
    private const int MaxLinesPerRequest = 4;
    private readonly EBAYClientOptions options = options.Value;

    public async Task<InventoryUpdateResult> UpdateInventoryAsync(
        CancellationToken cancellationToken = default)
    {
        using var lease = await executionLock.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            log.LogWarning("Eine eBay-Bestandsaktualisierung wird bereits ausgeführt.");
            return EmptyResult(InventoryUpdateRunStatus.AlreadyRunning);
        }

        IReadOnlyList<InventoryUpdateItem> sourceItems;
        try
        {
            sourceItems = await source.GetDifferencesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Die Bestandsabweichungen konnten nicht gelesen werden.");
            return EmptyResult(InventoryUpdateRunStatus.Failed, ex.Message);
        }

        if (sourceItems.Count == 0)
        {
            log.LogInformation("Es wurden keine eBay-Bestandsabweichungen gefunden.");
            return EmptyResult(InventoryUpdateRunStatus.NoChanges);
        }

        foreach (var item in sourceItems)
        {
            log.LogInformation(
                "eBay-Bestandsabweichung: ItemId {EbayItemId}, SKU {SellableSku}, gemeldet {ReportedQuantity}, Zielbestand {TargetQuantity}",
                item.EbayItemId,
                item.SellableSku,
                item.ReportedQuantity,
                item.TargetQuantity);
        }

        var (validItems, invalidCount) = Validate(sourceItems);
        var batches = new List<InventoryUpdateBatchResult>();

        foreach (var chunk in validItems.Chunk(MaxLinesPerRequest))
        {
            cancellationToken.ThrowIfCancellationRequested();
            batches.Add(await SendBatchAsync(chunk, cancellationToken).ConfigureAwait(false));
        }

        return BuildResult(sourceItems.Count, validItems.Count, invalidCount, batches);
    }

    private (List<InventoryUpdateItem> ValidItems, int InvalidCount) Validate(
        IReadOnlyList<InventoryUpdateItem> items)
    {
        var validItems = new List<InventoryUpdateItem>(items.Count);
        var seen = new HashSet<(string ItemId, string Sku)>();
        var invalidCount = 0;

        foreach (var item in items)
        {
            string? reason = null;
            if (string.IsNullOrWhiteSpace(item.EbayItemId))
            {
                reason = "Die eBay-Artikelnummer fehlt.";
            }
            else if (string.IsNullOrWhiteSpace(item.SellableSku))
            {
                reason = "Die SKU fehlt.";
            }
            else if (item.TargetQuantity < 0)
            {
                reason = "Der Zielbestand darf nicht negativ sein.";
            }
            else if (!seen.Add((item.EbayItemId, item.SellableSku)))
            {
                reason = "Die Kombination aus eBay-Artikelnummer und SKU ist doppelt vorhanden.";
            }

            if (reason is null)
            {
                validItems.Add(item);
                continue;
            }

            invalidCount++;
            log.LogError(
                "Ungültige eBay-Bestandszeile: ItemId {EbayItemId}, SKU {SellableSku}, Zielbestand {TargetQuantity}, Grund {Reason}",
                item.EbayItemId,
                item.SellableSku,
                item.TargetQuantity,
                reason);
        }

        return (validItems, invalidCount);
    }

    private async Task<InventoryUpdateBatchResult> SendBatchAsync(
        IReadOnlyCollection<InventoryUpdateItem> items,
        CancellationToken cancellationToken)
    {
        var messageId = $"inventory-update-{Guid.NewGuid():N}";
        using var request = InventoryUpdateRequests.CreateReviseInventoryStatus(
            items,
            messageId,
            options.TradingAPIVersion,
            options.EbayMarketPlaceID);
        using var client = httpClientFactory.GetEbayTradingApiHttpClient();

        log.LogInformation(
            "eBay-Bestandsgruppe {MessageId} mit {LineCount} Positionen wird gesendet.",
            messageId,
            items.Count);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            log.LogInformation(
                "eBay-Antwort für {MessageId}: HTTP {HttpStatusCode}, Inhalt {ResponseXml}",
                messageId,
                (int)response.StatusCode,
                responseXml);

            if (!response.IsSuccessStatusCode)
            {
                return FailedBatch(
                    messageId,
                    items.Count,
                    response.StatusCode,
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            ReviseInventoryStatusResponse ebayResponse;
            try
            {
                ebayResponse = InventoryUpdateRequests.DeserializeResponse(responseXml);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                log.LogError(ex, "Die eBay-Antwort für {MessageId} konnte nicht gelesen werden.", messageId);
                return FailedBatch(messageId, items.Count, response.StatusCode, ex.Message);
            }

            LogEbayErrors(messageId, ebayResponse.Errors);
            return MapResponse(messageId, items, response.StatusCode, ebayResponse);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            log.LogError(ex, "Zeitüberschreitung bei eBay-Bestandsgruppe {MessageId}; der Status ist ungewiss.", messageId);
            return UncertainBatch(messageId, items.Count, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            log.LogError(ex, "Transportfehler bei eBay-Bestandsgruppe {MessageId}; der Status ist ungewiss.", messageId);
            return UncertainBatch(messageId, items.Count, ex.Message);
        }
    }

    private void LogEbayErrors(string messageId, IReadOnlyCollection<EbayInventoryUpdateError> errors)
    {
        foreach (var error in errors)
        {
            log.LogWarning(
                "eBay-Meldung für {MessageId}: Code {ErrorCode}, Schweregrad {SeverityCode}, Kurzmeldung {ShortMessage}, Langmeldung {LongMessage}",
                messageId,
                error.ErrorCode,
                error.SeverityCode,
                error.ShortMessage,
                error.LongMessage);
        }
    }

    private static InventoryUpdateBatchResult MapResponse(
        string messageId,
        IReadOnlyCollection<InventoryUpdateItem> items,
        HttpStatusCode statusCode,
        ReviseInventoryStatusResponse response)
    {
        var ack = response.Ack?.Trim();
        if (string.Equals(ack, "Success", StringComparison.OrdinalIgnoreCase))
        {
            return Batch(messageId, response, InventoryUpdateBatchStatus.Success, items.Count, 0, 0, statusCode);
        }

        if (string.Equals(ack, "Warning", StringComparison.OrdinalIgnoreCase))
        {
            return Batch(messageId, response, InventoryUpdateBatchStatus.Warning, 0, items.Count, 0, statusCode);
        }

        if (string.Equals(ack, "PartialFailure", StringComparison.OrdinalIgnoreCase))
        {
            var returnedKeys = response.InventoryStatuses
                .Where(line => !string.IsNullOrWhiteSpace(line.ItemId) && !string.IsNullOrWhiteSpace(line.Sku))
                .Select(line => (line.ItemId!, line.Sku!))
                .ToHashSet();
            var successful = items.Count(item => returnedKeys.Contains((item.EbayItemId, item.SellableSku)));
            var failed = items.Count - successful;

            if (failed == 0)
            {
                failed = 1;
                successful = Math.Max(0, successful - 1);
            }

            return Batch(messageId, response, InventoryUpdateBatchStatus.PartialFailure, successful, 0, failed, statusCode);
        }

        return Batch(
            messageId,
            response,
            InventoryUpdateBatchStatus.Failure,
            0,
            0,
            items.Count,
            statusCode,
            string.IsNullOrWhiteSpace(ack) ? "Die eBay-Antwort enthält keinen Ack-Wert." : $"eBay Ack: {ack}");
    }

    private static InventoryUpdateBatchResult Batch(
        string messageId,
        ReviseInventoryStatusResponse response,
        InventoryUpdateBatchStatus status,
        int successful,
        int warnings,
        int failed,
        HttpStatusCode httpStatusCode,
        string? errorMessage = null) =>
        new(
            messageId,
            response.CorrelationId,
            status,
            successful + warnings + failed,
            successful,
            warnings,
            failed,
            0,
            response.Ack,
            (int)httpStatusCode,
            errorMessage,
            response.Errors);

    private static InventoryUpdateBatchResult FailedBatch(
        string messageId,
        int count,
        HttpStatusCode? statusCode,
        string errorMessage) =>
        new(messageId, null, InventoryUpdateBatchStatus.Failure, count, 0, 0, count, 0, null,
            statusCode is null ? null : (int)statusCode.Value, errorMessage, []);

    private static InventoryUpdateBatchResult UncertainBatch(
        string messageId,
        int count,
        string errorMessage) =>
        new(messageId, null, InventoryUpdateBatchStatus.Uncertain, count, 0, 0, 0, count, null, null, errorMessage, []);

    private static InventoryUpdateResult BuildResult(
        int sourceCount,
        int validCount,
        int invalidCount,
        IReadOnlyList<InventoryUpdateBatchResult> batches)
    {
        var successful = batches.Sum(batch => batch.SuccessfulLines);
        var warnings = batches.Sum(batch => batch.WarningLines);
        var failed = batches.Sum(batch => batch.FailedLines);
        var uncertain = batches.Sum(batch => batch.UncertainLines);
        var submitted = batches.Sum(batch => batch.RequestedLines);

        var status = failed > 0 || uncertain > 0 || invalidCount > 0
            ? successful + warnings > 0
                ? InventoryUpdateRunStatus.PartiallyFailed
                : InventoryUpdateRunStatus.Failed
            : warnings > 0
                ? InventoryUpdateRunStatus.CompletedWithWarnings
                : InventoryUpdateRunStatus.Completed;

        return new InventoryUpdateResult(
            status,
            sourceCount,
            validCount,
            invalidCount,
            submitted,
            successful,
            warnings,
            failed,
            uncertain,
            batches);
    }

    private static InventoryUpdateResult EmptyResult(
        InventoryUpdateRunStatus status,
        string? errorMessage = null) =>
        new(status, 0, 0, 0, 0, 0, 0, 0, 0, [], errorMessage);
}
