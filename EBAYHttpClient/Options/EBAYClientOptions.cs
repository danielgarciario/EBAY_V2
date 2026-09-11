namespace EBAYHttpClient.Options;

public sealed class EBAYClientOptions
{
    public static string EbayOptionsKey = "EBAYClient";

    public required string Appid { get; set; }
    public required string Certid { get; set; }
    public required string Devid { get; set; }
    public required string ClientBaseUrl { get; set; }
    public required string ContentLanguage { get; set; }
    public required string EbayMarketPlaceID { get; set; }
    public required string RedirectOauth2 { get; set; }
    public required string AuthToken { get; set; }

    public required string APIVersion { get; set; }
    public required string[] Scopes { get; set; }

}
