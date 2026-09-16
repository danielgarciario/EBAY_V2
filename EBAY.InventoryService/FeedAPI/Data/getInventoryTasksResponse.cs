using Newtonsoft.Json;

namespace EBAY.InventoryService.FeedAPI.Data;


/// <summary>
/// https://developer.ebay.com/develop/api/sell/feed_api#sell-feed_api-inventory_task-getinventorytasks
/// </summary>
public class getInventoryTasksResponse
{
    [JsonProperty("limit", Required = Required.Always)]
    public required int Limit { get; set; }
    [JsonProperty("offset", Required = Required.Always)]
    public required int Offset { get; set; }
    [JsonProperty("total", Required = Required.Always)]
    public required int Total { get; set; }
    [JsonProperty("href", Required = Required.Always)]
    public required string Href { get; set; }
    [JsonProperty("tasks", Required = Required.Always)]
    public InventoryTasksReponse[] Tasks { get; set; }
}

public sealed class InventoryTasksReponse
{
    [JsonProperty("taskId", Required = Required.Always)]
    public required string TaskId { get; set; }

    [JsonProperty("status", Required = Required.Always)]
    public required string Status { get; set; }

    [JsonProperty("feedType", Required = Required.Always)]
    public required string FeedType { get; set; }

    [JsonProperty("creationDate", Required = Required.Always)]
    public required DateTime CreationDate { get; set; }

    [JsonProperty("completionDate", Required = Required.Default)]
    public DateTime? CompletionDate { get; set; }

    [JsonProperty("detailHref", Required = Required.Default)]
    public string? DetailHref { get; set; }

    [JsonProperty("schemaVersion", Required = Required.Default)]
    public string? SchemaVersion { get; set; }
}
