using EBAY.Shared;

namespace EBAY.OrdersService.OrderImport.Data;


/// <summary>
/// https://developer.ebay.com/develop/api/sell/fulfillment_api#sell-fulfillment_api-order-getorders
/// </summary>
internal class getOrdersRequest
{

    public string FieldGroups => "TAX_BREAKDOWN";
    public string Filter { get; set; } = string.Empty;

    public int Limit { get; set; } = 50;

    public int Offset { get; set; } = 0;

    public string[]? OrderIDs { get; set; } = null;



    private getOrdersRequest()
    {

    }
    /// <summary>
    /// Genera el primer request para obtener 
    /// </summary>
    /// <param name="fromLocalTime"></param>
    /// <returns></returns>
    public static getOrdersRequest IncialRequest(DateTime fromLocalTime)
    {
        var utcstring = fromLocalTime.ToUTCJsonString();
        return new getOrdersRequest()
        {
            Filter = $"lastmodifieddate:%5B{utcstring}..%5D"
        };
    }

    public Dictionary<string, object?> ToParametros => new()
    {
        { "fieldGroups", this.FieldGroups },
        { "filter", this.Filter },
        { "limit", this.Limit },
        { "offset", this.Offset },
        { "ordersIds", this.OrderIDs }
    };




}


