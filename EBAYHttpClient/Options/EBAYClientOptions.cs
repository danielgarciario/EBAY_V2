namespace EBAYHttpClient.Options;

public sealed class EBAYClientOptions
{
    public static string EbayOptionsKey = "EBAYClient";

    public required string Appid { get; set; }
    public required string Certid { get; set; }
    public required string Devid { get; set; }

    public required string ClientBaseUrl { get; set; }
    public required string EbayAuthAPIBaseUrl { get; set; }

    public required string ContentLanguage { get; set; } = "DE-de";

    //AuthToken is the token es el que se usa para hacer las llamadas a la API de eBay de refresh token request.
    public required string AuthToken { get; set; }

    public required string APIVersion { get; set; }
    public required string[] Scopes { get; set; }

    public int ClientTimeoutSeconds { get; set; } = 120;

    public string Version => "1475";
    public required string EbayMarketPlaceID { get; set; } = "77";
    public string TradingAPIBaseUrl { get; set; } = "https://api.ebay.com/ws/api.dll";

    /// <summary>
    /// Trading API schema version sent in X-EBAY-API-COMPATIBILITY-LEVEL.
    /// This is separate from APIVersion to preserve existing configuration.
    /// </summary>
    public string TradingAPIVersion { get; set; } = "1477";

}
