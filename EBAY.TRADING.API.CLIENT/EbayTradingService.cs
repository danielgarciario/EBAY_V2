using EBAY.Trading.API;
using EBAY.TRADING.API.CLIENT.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EBAY.TRADING.API.CLIENT;

public sealed class EbayTradingService
{
    private readonly ILogger<EbayTradingService> log;
    private readonly EBayTradingApiContext apicontext;
    private readonly EBAYTradingOptions options;

    public EbayTradingService(ILogger<EbayTradingService> log, EBayTradingApiContext apicontext, IOptions<EBAYTradingOptions> options)
    {
        this.log = log;
        this.apicontext = apicontext;
        this.options = options.Value;
    }


    public async Task GetSellerList()
    {
        var client = this.apicontext.GetApiClient();

        var req = new GetSellerListRequest
        {
            RequesterCredentials = this.apicontext.GetRequesterCredentials(),
            GetSellerListRequest1 = new GetSellerListRequestType
            {
                Version = this.apicontext.GetVersion(),
                StartTimeFrom = DateTime.UtcNow.AddDays(-30),
                StartTimeTo = DateTime.UtcNow,
                Pagination = new PaginationType
                {
                    EntriesPerPage = 10,
                    PageNumber = 1
                }
            }
        };
        try
        {
            var respuesta = await client.GetSellerListAsync(req).ConfigureAwait(false);
            if (respuesta.GetSellerListResponse1.Ack == AckCodeType.Success)
            {
                log.LogInformation("GetSellerList successful. Total entries: {TotalEntries}", respuesta.GetSellerListResponse1.PaginationResult.TotalNumberOfEntries);
            }
            else
            {
                log.LogError("GetSellerList failed. Errors: {Errors}", string.Join(", ", respuesta.GetSellerListResponse1.Errors));
            }
        }
        catch (Exception e)
        {
            log.LogCritical($"Endpoint:{client.Endpoint.Address.Uri.ToString()}");
            log.LogCritical($"Binding:{JsonSerializer.Serialize(client.Endpoint.Binding)}");

            log.LogCritical($"Exception: {e.Message}");
            throw;
        }


    }


    public async Task<string> GetSellerListAsync()
    {
        var soap = $"""
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Header>
    <RequesterCredentials xmlns="urn:ebay:apis:eBLBaseComponents">
      <eBayAuthToken>{options.AuthToken}</eBayAuthToken>      
    </RequesterCredentials>
  </soap:Header>
  <soap:Body>
    <GetSellerListRequest xmlns="urn:ebay:apis:eBLBaseComponents">
      <Version>{options.Version}</Version>
      <StartTimeFrom>{DateTime.UtcNow.AddDays(-30):yyyy-MM-ddTHH:mm:ss.fffZ}</StartTimeFrom>
      <StartTimeTo>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</StartTimeTo>
      <Pagination>
        <EntriesPerPage>10</EntriesPerPage>
        <PageNumber>1</PageNumber>
      </Pagination>
    </GetSellerListRequest>
  </soap:Body>
</soap:Envelope>
""";

        using var client = new HttpClient();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.ebay.com/ws/api.dll");

        request.Content = new StringContent(
            soap,
            Encoding.UTF8,
            "text/xml");

        // IMPORTANTE
        request.Headers.Add("SOAPAction", "GetSellerList");

        request.Headers.Add("X-EBAY-API-CALL-NAME", "GetSellerList");
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", options.Version);
        request.Headers.Add("X-EBAY-API-SITEID", "0");

        request.Headers.Add("X-EBAY-API-APP-NAME", options.AppId);
        request.Headers.Add("X-EBAY-API-DEV-NAME", options.DevId);
        request.Headers.Add("X-EBAY-API-CERT-NAME", options.CertId);



        foreach (var h in request.Headers)
        {
            log.LogInformation($"{h.Key}={string.Join(",", h.Value)}");
        }


        var response = await client.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();
        log.LogInformation($"Status: {response.StatusCode}");
        log.LogInformation($"{content}");

        Console.WriteLine(content);

        //response.EnsureSuccessStatusCode();

        return content;
    }
}
