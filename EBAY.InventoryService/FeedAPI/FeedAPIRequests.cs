using EBAY.InventoryService.FeedAPI.Data;

namespace EBAY.InventoryService.FeedAPI;


internal static class FeedAPIRequests
{



    /// <summary>
    /// https://developer.ebay.com/develop/api/sell/feed_api#sell-feed_api-inventory_task-getinventorytasks
    /// </summary>
    /// <returns></returns>
    public static HttpRequestMessage getInventoryTasks(
        getInventoryTaskRequest getInventoryTask
        )
    {

        Dictionary<string, object?> parametros = new()
        {
            {"feed_type", getInventoryTask.FeedType},
            {"scheduled_id", getInventoryTask.ScheduledId},
            {"look_back_days", getInventoryTask.LookBackDays},
            {"date_range", getInventoryTask.DateRange},
            {"limit", getInventoryTask.Limit},
            {"offset", getInventoryTask.Offset}
        };


        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/sell/feed/v1/inventory_task?{BuildQueryString(parametros)}");
        request.Headers.Add("Accept", "application/json");


        return request;
    }
    /// <summary>
    /// https://developer.ebay.com/develop/api/sell/feed_api#sell-feed_api-inventory_task-createinventorytask
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public static HttpRequestMessage CreateInventoryTask(string taskId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/sell/feed/v1/inventory_task");
        request.Headers.Add("Accept", "application/json");
        request.Content =
        return request;
    }


    private static string BuildQueryString(Dictionary<string, object?> parameters)
    {
        var queryParams = new List<string>();
        foreach (var param in parameters)
        {
            if (param.Value != null)
            {
                string value = param.Value is DateRange dateRange ? dateRange.ToString() : param.Value.ToString()!;
                queryParams.Add($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
            }
        }
        return string.Join("&", queryParams);
    }

}
