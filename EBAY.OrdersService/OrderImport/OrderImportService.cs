using EBAY.DatabaseConnection;
using EBAY.Shared;
using EBAYHttpClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace EBAY.OrdersService.OrderImport;

public sealed class OrderImportService(
    ILogger<OrderImportService> log,
    IEbayDatabaseConnectionFactory connectionFactory,
    IEBayHttpClientFactory httpClientFactory,
    IOptions<OrderImportOptions> options,
    OrderImportExecutionLock executionLock) : IOrderImportService
{
    private static readonly TimeZoneInfo BerlinTimeZone = GetBerlinTimeZone();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OrderImportOptions options = options.Value;

    public Task<OrderImportRunResult> ImportRecentOrdersAsync(CancellationToken cancellationToken = default)
    {
        var fromLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BerlinTimeZone)
            .DateTime
            .AddMinutes(-options.PollingLookbackMinutes);
        return ImportModifiedOrdersAsync(fromLocal, cancellationToken);
    }

    public async Task<OrderImportRunResult> ImportModifiedOrdersAsync(
        DateTime fromLocal,
        CancellationToken cancellationToken = default)
    {
        var normalizedFromLocal = DateTime.SpecifyKind(fromLocal, DateTimeKind.Unspecified);
        using var lease = await executionLock.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            log.LogWarning("Ein eBay-Bestellimport wird bereits ausgeführt.");
            return new OrderImportRunResult(
                OrderImportRunStatus.AlreadyRunning,
                normalizedFromLocal,
                NowLocal(),
                0,
                0,
                0,
                [],
                "Ein Bestellimport wird bereits ausgeführt.");
        }

        try
        {
            var s = await ImportCoreAsync(normalizedFromLocal, cancellationToken).ConfigureAwait(false);
            return s.Match(
                r => r,
                e => throw new Exception(e.MsgErr)
                );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Der eBay-Bestellimport ist fehlgeschlagen.");
            return new OrderImportRunResult(
                OrderImportRunStatus.Failed,
                normalizedFromLocal,
                NowLocal(),
                0,
                0,
                0,
                [],
                ex.Message);
        }
    }

    private async Task<Result<OrderImportRunResult, MiError>> ImportCoreAsync(DateTime fromLocal, CancellationToken cancellationToken)
    {

        var visitedRequestUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string actualUri;
        var batchIds = new List<long>();
        var pagesRead = 0;
        var ordersReceived = 0;
        var ordersImportedOrUpdated = 0;
        var final = false;

        using var client = httpClientFactory.GetEbayHttpClient();
        HttpRequestMessage? httprequest = OrderManagementFullfillmentRequests.GetOrders(fromLocal);

        while (!final)
        {
            cancellationToken.ThrowIfCancellationRequested();
            actualUri = RequestUriText(httprequest.RequestUri!);
            if (!visitedRequestUris.Add(actualUri))
            {
                return MiError.FromMsg($"El API de EBAY nos manda en circulos: {actualUri} esta repetida");
            }

            var startedAtLocal = NowLocal();

            var process = await ProcesaHttpRequestGetOrders(client, httprequest, cancellationToken).ConfigureAwait(false);
            var grabar = await process.BindAsync(
                  async ((GetOrdersResponseDto order, string Content, Uri enviado, HttpRequestMessage? siguiente) x) =>
            {
                httprequest = x.siguiente;
                return await AlmacenaRequestGetOrders(x.order, x.Content, x.enviado, startedAtLocal, cancellationToken).ConfigureAwait(false);
            }
            ).ConfigureAwait(false);

            // Informar y preprar la siguiente vuelta.
            grabar.Tap((s) =>
            {
                batchIds.Add(s.ImportBatchId);
                pagesRead++;
                ordersReceived += s.ReceivedOrderCount;
                ordersImportedOrUpdated += s.ImportedOrUpdatedOrderCount;
                final = (httprequest is null);
                log.LogInformation(
                "eBay-Bestellseite importiert: Batch {ImportBatchId}, empfangen {ReceivedOrderCount}, neu oder aktualisiert {ImportedOrUpdatedOrderCount}.",
                s.ImportBatchId,
                s.ReceivedOrderCount,
                s.ImportedOrUpdatedOrderCount);
            });
            if (grabar.IsErr)
            {
                return grabar.Error;
            }
        }
        return new OrderImportRunResult(
            ordersReceived == 0 ? OrderImportRunStatus.NoOrders : OrderImportRunStatus.Completed,
            fromLocal,
            NowLocal(),
            pagesRead,
            ordersReceived,
            ordersImportedOrUpdated,
            batchIds);






    }

    /// <summary>
    /// realiza el envio a GetOrders y hace un dispose al request si se ha ejecutado correctamente.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<Result<(GetOrdersResponseDto, string, Uri, HttpRequestMessage?), MiError>> ProcesaHttpRequestGetOrders(HttpClient client, HttpRequestMessage? request, CancellationToken ct)
    {
        try
        {
            if (request is null) return MiError.FromMsg("Request está vacia");
            Uri requestedUri = new Uri(request.RequestUri!.ToString(), UriKind.RelativeOrAbsolute);
            using var response = await client.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return await MiError.FromHttpResponse(response);
            }
            var sourceContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            var page = JsonSerializer.Deserialize<GetOrdersResponseDto>(sourceContent, JsonOptions);

            if (page is null)
            {
                log.LogCritical($"Contenido no deserializable: {sourceContent}");
                return MiError.FromMsg("No he podido deserializar el contenido.");
            }
            // Ojo aqui hago dispose del request porque todo ha ido bien...
            request.Dispose();
            request = null;
            if (!string.IsNullOrWhiteSpace(page.Next))
            {
                request = new HttpRequestMessage(HttpMethod.Get, page.Next);
            }
            return (page, sourceContent, requestedUri, request);

        }
        catch (InvalidOperationException InvOpEx)
        {
            log.LogCritical(InvOpEx, "Invalid operation exception on EBAY GetOrders Request");
            return MiError.FromException(InvOpEx);
        }
        catch (HttpRequestException htpreqex)
        {
            log.LogCritical(htpreqex, "Invalid operation exception on EBAY GetOrders Request");
            return MiError.FromException(htpreqex);
        }
        catch (OperationCanceledException opcanex)
        {
            log.LogCritical(opcanex, "operation cancelled by user in EBAY GetOrders Request");
            return MiError.FromException(opcanex);
        }
        catch (ArgumentNullException ArtNullEx)
        {
            log.LogCritical(ArtNullEx, "ArgumentNull Exception on EBAY GetOrders Request");
            return MiError.FromException(ArtNullEx);
        }
        catch (JsonException JsonEx)
        {
            log.LogCritical(JsonEx, "ArgumentNull Exception on EBAY GetOrders Request");
            return MiError.FromException(JsonEx);
        }
        catch (Exception ex)
        {
            log.LogCritical(ex, "General Excepcion in EBAY Get Orders Request");
            return MiError.FromException(ex);
        }

    }


    private async Task<Result<ImportOrdersSqlResult, MiError>> AlmacenaRequestGetOrders(GetOrdersResponseDto page, string sourceContent, Uri requestedUri, DateTime startedAtLocal, CancellationToken cancellationToken)
    {
        var completedAtLocal = NowLocal();
        try
        {
            var tables = OrderImportDataTableBuilder.Build(page);
            var sqlResult = await PersistPageAsync(
                requestedUri,
                sourceContent,
                startedAtLocal,
                completedAtLocal,
                tables,
                cancellationToken).ConfigureAwait(false);
            return sqlResult;
        }
        catch (Exception ex)
        {

            log.LogCritical(ex, "Grabando en la base de datos");
            return MiError.FromException(ex);
        }


    }


    private async Task<ImportOrdersSqlResult> PersistPageAsync(
        Uri requestUri,
        string sourceContent,
        DateTime startedAtLocal,
        DateTime completedAtLocal,
        OrderImportTables tables,
        CancellationToken cancellationToken)
    {
        var import = new ImportOrdersCommand
        {
            Source = "Polling",
            SourceReference = RequestUriText(requestUri),
            SourceContent = sourceContent,
            SourceContentSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceContent))).ToLowerInvariant(),
            StartedAtLocal = startedAtLocal,
            CompletedAtLocal = completedAtLocal,
            ImportedAtLocal = NowLocal(),
            Orders = tables.Orders,
            ShippingAddresses = tables.ShippingAddresses,
            LineItems = tables.LineItems,
            VariationAspects = tables.VariationAspects,
            Promotions = tables.Promotions,
            Taxes = tables.Taxes,
            Refunds = tables.Refunds
        };

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = OrderImportSqlCommandFactory.CreateImportCommand(
            connection,
            import,
            connectionFactory.CommandTimeOutSeconds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Der Bestellimport hat kein Ergebnis zurückgegeben.");
        }

        return new ImportOrdersSqlResult(
            reader.GetInt64(reader.GetOrdinal("ImportBatchId")),
            reader.GetInt32(reader.GetOrdinal("ReceivedOrderCount")),
            reader.GetInt32(reader.GetOrdinal("ImportedOrUpdatedOrderCount")));
    }

    private Uri CreateFirstRequestUri(DateTime fromLocal)
    {
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fromLocal, DateTimeKind.Unspecified), BerlinTimeZone);
        var timestamp = fromUtc.ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        var filter = $"lastmodifieddate:[{timestamp}..]";
        var relativeUri = $"/sell/fulfillment/v1/order?filter={Uri.EscapeDataString(filter)}&fieldGroups=TAX_BREAKDOWN&limit={options.PageSize}&offset=0";
        return new Uri(relativeUri, UriKind.Relative);
    }

    private static Uri? ToNextRequestUri(string? next)
    {
        if (string.IsNullOrWhiteSpace(next))
        {
            return null;
        }

        return Uri.TryCreate(next, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(next, UriKind.Relative);
    }

    private static string RequestUriText(Uri uri) => uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.OriginalString;

    private static DateTime NowLocal() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BerlinTimeZone).DateTime;

    private static TimeZoneInfo GetBerlinTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
    }
}
