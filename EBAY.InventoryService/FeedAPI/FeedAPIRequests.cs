using EBAY.InventoryService.FeedAPI.Data;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

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
    /// obtiene el estado de un Task
    /// Espero formato asi: /sell/feed/v1/task/task-20-29399840325634
    /// </summary>
    /// <param name="relativePathToTaskId"></param>
    /// <returns></returns>
    public static HttpRequestMessage getInventoryTask(string relativePathToTaskId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, relativePathToTaskId);
        request.Headers.Add("Accept", "application/json");
        return request;
    }
    /// <summary>
    /// https://developer.ebay.com/develop/api/sell/feed_api#sell-feed_api-inventory_task-getinventorytask
    /// </summary>
    /// <param name="TaskId"></param>
    /// <returns></returns>
    public static HttpRequestMessage getInventoryTaskFromTaskId(string TaskId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/sell/feed/v1/inventory_task/{TaskId}");
        request.Headers.Add("Accpet", "application/json");
        return request;

    }


    /// <summary>
    /// https://developer.ebay.com/develop/api/sell/feed_api#sell-feed_api-inventory_task-createinventorytask
    /// </summary>
    /// 
    /// <returns></returns>
    public static HttpRequestMessage CreateInventoryTask()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/sell/feed/v1/inventory_task");
        request.Headers.Add("Accept", "application/json");
        var citr = new createInventoryTaskRequest();
        request.Content = new StringContent(JsonConvert.SerializeObject(citr), encoding: Encoding.UTF8, mediaType: MediaTypeHeaderValue.Parse("application/json"));
        return request;
    }

    /// <summary>
    /// https://developer.ebay.com/develop/api/sell/feed_api#sell-feed_api-task-getresultfile
    /// </summary>
    /// <param name="itr"></param>
    /// <returns></returns>
    public static HttpRequestMessage DownloadResultFile(InventoryTasksReponse itr)
    {

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/sell/feed/v1/task/{itr.TaskId}/download_result_file");
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
