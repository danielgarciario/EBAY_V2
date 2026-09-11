using EBAY.Trading.API;
using EBAY.TRADING.API.CLIENT.Inspectors;
using EBAY.TRADING.API.CLIENT.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EBAY.TRADING.API.CLIENT;

public sealed class EBayTradingApiContext
{
    private readonly ILogger<EBayTradingApiContext> log;
    private readonly EBAYTradingOptions options;
    private CustomSecurityHeaderType _cred;
    private string _version;



    public EBayTradingApiContext(
        ILogger<EBayTradingApiContext> log,
        IOptions<EBAYTradingOptions> options
        )
    {
        this.log = log;
        this.options = options.Value;
        this._cred = new CustomSecurityHeaderType
        {
            Credentials = new UserIdPasswordType
            {
                AppId = this.options.AppId,
                DevId = this.options.DevId,
                AuthCert = this.options.CertId,
            },
            eBayAuthToken = this.options.AuthToken
        };
        this._version = this.options.Version;
    }



    public CustomSecurityHeaderType GetRequesterCredentials()
    {
        return this._cred;
    }
    public string GetVersion()
    {
        return this._version;
    }

    public eBayAPIInterfaceClient GetApiClient()
    {

        var client = new eBayAPIInterfaceClient(
            eBayAPIInterfaceClient.EndpointConfiguration.eBayAPI,
            remoteAddress: options.BaseApiUrl);
        client.Endpoint.EndpointBehaviors.Add(new SoapLoggerBehaviour(this.log));
        return client;
    }

}
