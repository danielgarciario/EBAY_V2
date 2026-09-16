using EBAYHttpClient.DATA;
using EBAYHttpClient.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Web;

namespace EBAYHttpClient.Services;


public interface IOAuthTokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}




/// <summary>
/// Siguiendo las instrucciones de:
/// https://developer.ebay.com/develop/guides/sell/authorization
/// The refresh token request.
/// </summary>
public sealed class OAuthTokenService : IOAuthTokenService
{
    private readonly ILogger<OAuthTokenService> log;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly EBAYClientOptions options;
    private UserTokenResponse? _cachedTokenResponse;

    private readonly SemaphoreSlim _semaforo = new SemaphoreSlim(1, 1);


    public OAuthTokenService(
        ILogger<OAuthTokenService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<EBAYClientOptions> options
        )
    {
        this.log = logger;
        this.httpClientFactory = httpClientFactory;
        this.options = options.Value;
    }


    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        log.LogInformation("Necesito un Token de Acceso.");
        await _semaforo.WaitAsync(cancellationToken);
        try
        {
            if (_cachedTokenResponse != null && !_cachedTokenResponse.IsExpired)
            {
                log.LogInformation("Usando token de acceso en cache.");
                return _cachedTokenResponse.AccessToken;
            }
            log.LogInformation("Hay que pedirle a EBAY un token nuevo.");
            return await _GetAccessTokenAsync(cancellationToken);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    private async Task<string> _GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(Constantes.HttpclientOauth2);
        var requestMessage = GeneraRequestMessage();
        var response = await client.SendAsync(requestMessage, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            log.LogError("Error al obtener el token de acceso. StatusCode: {StatusCode}, ReasonPhrase: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            throw new Exception($"Error al obtener el token de acceso. StatusCode: {response.StatusCode}, ReasonPhrase: {response.ReasonPhrase}");
        }
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonConvert.DeserializeObject<UserTokenResponse>(content);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            log.LogError("Error al deserializar la respuesta del token de acceso. Content: {Content}", content);
            throw new Exception($"Error al deserializar la respuesta del token de acceso. Content: {content}");
        }
        _cachedTokenResponse = tokenResponse;
        return tokenResponse.AccessToken;
    }


    private HttpRequestMessage GeneraRequestMessage()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/identity/v1/oauth2/token");
        requestMessage.Content = GetBodyRequestMessage();
        requestMessage.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
        return requestMessage;
    }

    private FormUrlEncodedContent GetBodyRequestMessage()
    {
        //scope = a URL-encoded string of space separated scopes.
        var body = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", options.AuthToken },
            { "scope", HttpUtility.UrlEncode(string.Join(" ", options.Scopes)) }
        };
        return new FormUrlEncodedContent(body);
    }

}
