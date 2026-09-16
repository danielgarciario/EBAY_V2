using Newtonsoft.Json;

namespace EBAYHttpClient.DATA;
/// <summary>
/// Esto es lo que contesta el endpoint de eBay cuando se hace la petición para obtener un token de usuario.
/// </summary>
internal class UserTokenResponse
{

    private DateTime _creadoAt { get; init; }

    [JsonProperty("access_token", Required = Required.Always)]
    public required string AccessToken { get; set; }

    [JsonProperty("expires_in", Required = Required.Always)]
    public required int ExpiresIn { get; set; }

    [JsonProperty("token_type", Required = Required.Always)]
    public required string TokenType { get; set; }

    public bool IsExpired => _creadoAt.AddSeconds(ExpiresIn - 60) < DateTime.UtcNow;

    public UserTokenResponse()
    {
        _creadoAt = DateTime.UtcNow;
    }

}
