using Newtonsoft.Json;

namespace EBAY.InventoryService.FeedAPI.Data
{

    /// <summary>
    /// https://developer.ebay.com/develop/api/sell/feed_api#sell-feed_api-inventory_task-createinventorytask.createinventorytaskrequest.feedtype
    /// </summary>
    public sealed class createInventoryTaskrequest
    {


        [JsonProperty("feedType", Required = Required.Always)]
        public static string FeedType => "LMS_ACTIVE_INVENTORY_REPORT";

        [JsonProperty("filterCriteria", Required = Required.Default)]
        public Filtercriteria? FilterCriteria { get; set; }

        [JsonProperty("schemaVersion", Required = Required.Always)]
        public static string SchemaVersion => "1.0";






    }
    public sealed class Filtercriteria
    {
        [JsonProperty("listingFormat", Required = Required.Default)]
        public string? ListingFormat { get; set; }
    }
}
