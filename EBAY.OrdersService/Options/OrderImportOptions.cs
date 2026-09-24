namespace EBAY.OrdersService.OrderImport;

public sealed class OrderImportOptions
{
    public const string SectionName = "EBAYOrders";

    public int PollingLookbackMinutes { get; set; } = 15;

    public int PageSize { get; set; } = 200;
}
