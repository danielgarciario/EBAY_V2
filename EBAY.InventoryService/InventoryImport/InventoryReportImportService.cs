using EBAY.DatabaseConnection;
using EBAY.InventoryService.FeedAPI;
using EBAY.InventoryService.FeedAPI.Data;
using EBAY.Shared;
using EBAYHttpClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace EBAY.InventoryService.InventoryImport;

public sealed class InventoryReportImportService(
    ILogger<InventoryReportImportService> log,
    IEbayDatabaseConnectionFactory connectionFactory,
    InventoryReportParser parser,
    IEBayHttpClientFactory httpclientfactory
    ) : IInventoryReportImportService
{
    private readonly ILogger<InventoryReportImportService> log = log;
    private readonly IEbayDatabaseConnectionFactory connectionFactory = connectionFactory;
    private readonly InventoryReportParser parser = parser;
    private readonly IEBayHttpClientFactory httpclientfactory = httpclientfactory;

    private const int INTERVALO_CHECKEO_TARA_MILISEGUNDOS = 5_000;
    private const int MAX_NUMERO_INTENTOS = 100;

    /// <summary>
    /// Creo una solicitud de InventoryReport estandar.
    /// Devuelve el path relativo que tenemos que usar para consultar el estado
    /// Formato de salida: /sell/feed/v1/task/task-20-29399840325634
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<Result<string, MiError>> CreateInventoryReport(CancellationToken ct = default)
    {
        var msg = FeedAPIRequests.CreateInventoryTask();
        using (var httpclient = httpclientfactory.GetEbayHttpClient())
        {
            try
            {
                log.LogInformation("Solicitando un nuevo Inventory Report");
                var response = await httpclient.SendAsync(msg, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return await MiError.FromHttpResponse(response).ConfigureAwait(false);
                log.LogInformation($"Recibido Ok.Status:{response.StatusCode}. Location:{response.Headers.Location?.AbsolutePath ?? "-Nada-"}");
                return response.Headers.Location?.AbsolutePath ?? string.Empty;

            }
            catch (Exception ex)
            {
                log.LogCritical(ex, "Create Inventory Report");
                return MiError.FromException(ex);
            }

        }
    }


    public async Task<bool> LoadInventory(CancellationToken ct = default)
    {
        var nir = await CreateInventoryReport(ct).ConfigureAwait(false);
        var comp = await nir.BindAsync(s => EsperarCompletion(s, ct)).ConfigureAwait(false);
        var dwn = await comp.BindAsync(itr => DownloadAndExtractZIPFile(itr, ct));
        var salida = await dwn.MatchAsync<bool>(
            async ((InventoryTasksReponse itr, Stream streamxml) entrada) =>
            {
                return await Grabalo(entrada.itr, entrada.streamxml).ConfigureAwait(false);
            }
            ,
            e_ =>
            {
                log.LogCritical(e_.MsgErr);
                log.LogCritical("Final");
                return Task.FromResult(false);
            }

            );
        return salida;

    }


    public async Task<bool> LoadInventory(string taskId, CancellationToken ct = default)
    {
        var state = await GetInventoryTaskStateFromTaskID(taskId, ct);
        var dwn = await state.BindAsync(itr => DownloadAndExtractZIPFile(itr, ct));
        var salida = await dwn.MatchAsync<bool>(
            async ((InventoryTasksReponse itr, Stream streamxml) entrada) =>
            {
                return await Grabalo(entrada.itr, entrada.streamxml).ConfigureAwait(false);
            }
            ,
            e_ =>
            {
                log.LogCritical(e_.MsgErr);
                log.LogCritical("Final");
                return Task.FromResult(false);
            }

            );
        return salida;
    }

    private async Task<bool> Grabalo(
            InventoryTasksReponse itr,
            Stream streamxml
            )
    {
        InventoryReportImportRequest irir = new()
        {
            TaskId = Guid.NewGuid().ToString(),
            SourceFileName = itr.TaskId,
            //SourceFileSha256 = BitConverter.ToString(SHA256.HashData(bytesxml)).Replace("-", ""),
            SourceFileSha256 = await CalculateSha256Async(streamxml),
            StartedAtLocal = itr.CreationDate.ToLocalTime(),
            CompletedAtLocal = itr.CompletionDate?.ToLocalTime(),
            ImportedAtLocal = DateTime.Now,
        };
        var irss = await ImportXmlAsync(
            streamxml,
            irir, cancellationToken: CancellationToken.None
            ).ConfigureAwait(false);
        log.LogInformation($"ImportedBatchId:{irss.ImportBatchId}. SKUs:{irss.SkuDetailsCount}. Sellable:{irss.SellableCount}");
        // Es un fire and forget.
        await EjecutaValidacionEnEfAAsync();
        return true;
    }


    private static async Task<string> CalculateSha256Async(Stream stream)
    {
        long originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            using var sha256 = SHA256.Create();
            byte[] hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }


    private async Task<Result<InventoryTasksReponse, MiError>> EsperarCompletion(string relativePathToTaskId, CancellationToken ct = default)
    {
        var taskid = getTaskIDFromRelativePath(relativePathToTaskId);
        if (string.IsNullOrWhiteSpace(relativePathToTaskId) ||
                string.IsNullOrWhiteSpace(taskid))
        {
            log.LogCritical("La creacion de la tarea puede haber fallado:{tarea}", relativePathToTaskId);
            return MiError.FromMsg($"La tarea esta en blanco o mal formada:{relativePathToTaskId}");
        }

        int reintentos = 1;
        bool final = false;
        while (!final && reintentos <= MAX_NUMERO_INTENTOS)
        {
            /// Esperamos unos segundos.
            log.LogInformation($"{reintentos}. Intento: Esperamos.");
            await Task.Delay(INTERVALO_CHECKEO_TARA_MILISEGUNDOS, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) return MiError.FromMsg("User Cancelled");
            var resp = await GetInventoryTaskState(relativePathToTaskId, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) return MiError.FromMsg("User Cancelled");
            if (resp.IsErr) return resp.Error;
            if (resp.Value.Status.Equals("COMPLETED")) return resp;
            log.LogInformation($"Ha respondido con Status:{resp.Value.Status}");
            reintentos++;
        }
        return MiError.FromMsg($"Alcanzado el numero maximo de reintentos: {MAX_NUMERO_INTENTOS} en {INTERVALO_CHECKEO_TARA_MILISEGUNDOS}ms");
    }


    private static string getTaskIDFromRelativePath(string relativePathToTaskId)
    {
        var partes = relativePathToTaskId.Split('/');
        return partes.FirstOrDefault(x => x.StartsWith("task-")) ?? string.Empty;
    }
    /// <summary>
    /// Pregunta a EBAY por el estado de la tarea.
    /// </summary>
    /// <param name="relativePathToTaskId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<Result<InventoryTasksReponse, MiError>> GetInventoryTaskState(string relativePathToTaskId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePathToTaskId)) return MiError.FromMsg("relativePathToTaksId esta vacio.");
        var taskid = getTaskIDFromRelativePath(relativePathToTaskId);
        if (string.IsNullOrWhiteSpace(taskid))
        {
            log.LogCritical($"ruta no esperada:'{relativePathToTaskId}'");
            return MiError.FromMsg($"Ruta no contiene TaskID:{relativePathToTaskId}");
        }
        var msg = FeedAPIRequests.getInventoryTask(relativePathToTaskId);
        using (var httpclient = httpclientfactory.GetEbayHttpClient())
        {
            try
            {
                log.LogInformation($"Comprobando el estado de TaskID:{taskid}");
                var response = await httpclient.SendAsync(msg, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return await MiError.FromHttpResponse(response).ConfigureAwait(false);
                var contenido = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var salida = JsonConvert.DeserializeObject<InventoryTasksReponse>(contenido);
                if (salida is null)
                {
                    log.LogCritical("No he podido deseralizar InventoryTaskResponse");
                    log.LogCritical($"Contenido:{contenido}");
                    return MiError.FromMsg("No he podido deserializar InventoryTaskResponse");
                }
                return salida;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex, "GetInventoryTaskState");
                return MiError.FromException(ex);
            }
        }
    }


    private async Task<Result<InventoryTasksReponse, MiError>> GetInventoryTaskStateFromTaskID(string TaskId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(TaskId)) return MiError.FromMsg("TaksId esta vacio.");
        var taskid = TaskId;

        var msg = FeedAPIRequests.getInventoryTaskFromTaskId(taskid);
        using (var httpclient = httpclientfactory.GetEbayHttpClient())
        {
            try
            {
                log.LogInformation($"Comprobando el estado de TaskID:{taskid}");
                var response = await httpclient.SendAsync(msg, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return await MiError.FromHttpResponse(response).ConfigureAwait(false);
                var contenido = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var salida = JsonConvert.DeserializeObject<InventoryTasksReponse>(contenido);
                if (salida is null)
                {
                    log.LogCritical("No he podido deseralizar InventoryTaskResponse");
                    log.LogCritical($"Contenido:{contenido}");
                    return MiError.FromMsg("No he podido deserializar InventoryTaskResponse");
                }
                return salida;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex, "GetInventoryTaskState");
                return MiError.FromException(ex);
            }
        }
    }

    private async Task<Result<(InventoryTasksReponse, Stream), MiError>>
    DownloadAndExtractZIPFile(
        InventoryTasksReponse itr,
        CancellationToken ct = default)
    {
        try
        {
            log.LogInformation(
                "Procedemos a descargar el archivo: {TaskId}. Estado: {Status}",
                itr.TaskId,
                itr.Status);

            using var httpClient = httpclientfactory.GetEbayHttpClient();

            var request = FeedAPIRequests.DownloadResultFile(itr);

            using var response = await httpClient
                .SendAsync(request, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return await MiError.FromHttpResponse(response);

            await using var contentStream = await response.Content
                .ReadAsStreamAsync(ct)
                .ConfigureAwait(false);

            using var zipArchive = new ZipArchive(
                contentStream,
                ZipArchiveMode.Read);

            if (zipArchive.Entries.Count != 1)
            {
                string err = $"El ZIP tiene {zipArchive.Entries.Count} ficheros";

                log.LogCritical(err);

                return MiError.FromMsg(err);
            }

            var xmlEntry = zipArchive.Entries.First();

            await using var zipEntryStream = xmlEntry.Open();

            byte[] xmlBytes = await ReadFullyAsync(zipEntryStream);

            // Creamos un stream independiente del ZipArchive
            Stream xmlStream = new MemoryStream(xmlBytes);

            return (itr, xmlStream);
        }
        catch (Exception ex)
        {
            log.LogCritical(ex, "DownloadingAndExtractingZipFile");
            return MiError.FromException(ex);
        }
    }




    private async Task<byte[]> ReadFullyAsync(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            await input.CopyToAsync(ms);
            return ms.ToArray();
        }
    }



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


    public async Task EjecutaValidacionEnEfAAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var commandvalidate = InventoryImportSqlCommandFactory.CheckedImportAgainstEFACommand(
            connection,
            connectionFactory.CommandTimeOutSeconds);
        await using var readervalidate = await commandvalidate.ExecuteReaderAsync(ct).ConfigureAwait(false);
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

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = InventoryImportSqlCommandFactory.CreateImportCommand(
            connection,
            import,
            connectionFactory.CommandTimeOutSeconds);

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
            Source = NormalizeOptional(request.Source) ?? "SellFeedApi",
            FeedType = NormalizeOptional(request.FeedType) ?? "LMS_ACTIVE_INVENTORY_REPORT",
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
