using EBAYHttpClient.Handler;
using EBAYHttpClient.Options;
using EBAYHttpClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace EBAYHttpClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEbayHttpClient(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<EBAYClientOptions>()
            .Bind(config.GetSection(EBAYClientOptions.EbayOptionsKey))
            .Validate(
                options => Uri.TryCreate(options.ClientBaseUrl, UriKind.Absolute, out _),
                "Die eBay-REST-API-URL ist ungültig.")
            .Validate(
                options => Uri.TryCreate(options.EbayAuthAPIBaseUrl, UriKind.Absolute, out _),
                "Die eBay-OAuth-API-URL ist ungültig.")
            .Validate(
                options => Uri.TryCreate(options.TradingAPIBaseUrl, UriKind.Absolute, out _),
                "Die eBay-Trading-API-URL ist ungültig.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.TradingAPIVersion),
                "Die eBay-Trading-API-Version ist erforderlich.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.EbayMarketPlaceID),
                "Die eBay-Site-ID ist erforderlich.")
            .Validate(
                options => options.ClientTimeoutSeconds > 0,
                "Das eBay-HTTP-Zeitlimit muss größer als null sein.")
            .ValidateOnStart();

        services.AddSingleton<IOAuthTokenService, OAuthTokenService>();

        // Defino el cliente para llamar a la Authentication API de eBay
        services.AddHttpClient(Constantes.HttpclientOauth2, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EBAYClientOptions>>().Value;
            client.BaseAddress = new Uri(options.EbayAuthAPIBaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GetAuthCredentials(options));

            //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{options.Appid}:{options.Certid}")));
        });
        // Defino el cliente para hacer el resto de llamdas al API de eBay, con el token de autenticación que se obtiene del cliente anterior
        services.AddHttpClient(Constantes.HttpclientProd, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EBAYClientOptions>>().Value;
            client.BaseAddress = new Uri(options.ClientBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.ClientTimeoutSeconds);
        }).AddHttpMessageHandler(sp => new EBAYHeadersDelegateHandler(sp.GetRequiredService<IOAuthTokenService>()));


        services.AddSingleton<IEBayHttpClientFactory, EBayHttpClientFactory>();


        return services;
    }

    private static string GetAuthCredentials(EBAYClientOptions opts)
    {
        return ToEncode64($"{opts.Appid}:{opts.Certid}");
    }


    private static string ToEncode64(string toencode) => Convert.ToBase64String(Encoding.ASCII.GetBytes(toencode));

}
