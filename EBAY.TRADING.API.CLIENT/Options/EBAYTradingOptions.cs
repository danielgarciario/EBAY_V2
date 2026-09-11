namespace EBAY.TRADING.API.CLIENT.Options;

public sealed class EBAYTradingOptions
{

    public static string EBAYTradingOptionsKey => "EBAYTrading";

    public required string AppId { get; set; }
    public required string DevId { get; set; }
    public required string CertId { get; set; }
    public required string AuthToken { get; set; }
    /// <summary>
    /// Esta es lo que dice en el momento de la descarga de la 
    /// </summary>
    public string Version => "1475";
    public string BaseApiUrl => "https://api.ebay.com/ws/api.dll";

}
