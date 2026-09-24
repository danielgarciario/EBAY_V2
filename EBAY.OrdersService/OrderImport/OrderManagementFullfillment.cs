using EBAY.OrdersService.OrderImport.Data;

namespace EBAY.OrdersService.OrderImport;

internal static class OrderManagementFullfillmentRequests
{
    /// <summary>
    /// https://developer.ebay.com/develop/api/sell/fulfillment_api#sell-fulfillment_api-order-getorders
    /// </summary>
    /// <param name="fromLocal"></param>
    /// <param name=""></param>
    /// <returns></returns>
    public static HttpRequestMessage GetOrders(DateTime fromLocal)
    {
        var qp = getOrdersRequest.IncialRequest(fromLocal);
        var req = new HttpRequestMessage(HttpMethod.Get, $"/sell/fulfillment/v1/order?{BuildQueryString(qp.ToParametros)}");
        req.Headers.Add("Accept", "application/json");
        return req;

    }



    private static string BuildQueryString(Dictionary<string, object?> parameters)
    {
        var queryParams = new List<string>();
        foreach (var param in parameters)
        {
            if (param.Value != null)
            {
                string value = param.Value.ToString()!;
                queryParams.Add($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
            }
        }
        return string.Join("&", queryParams);
    }
}
