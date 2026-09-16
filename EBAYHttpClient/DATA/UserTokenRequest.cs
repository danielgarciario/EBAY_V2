using Newtonsoft.Json;

namespace EBAYHttpClient.DATA;

internal sealed class UserTokenRequest
{
    [JsonProperty("grant_type", Required = Required.Always)]
    public required string Grant_type { get; set; } = "refresh_token";

    [JsonProperty("refresh_token", Required = Required.Always)]
    public required string RefreshToken { get; set; }

    [JsonProperty("scope", Required = Required.Default)]
    public string? Scope { get; set; }

}
