namespace EBAY.InventoryService.FeedAPI.Data;

public record DateRange(DateTime? from, DateTime? to)
{
    public override string ToString()
    {
        var fromStr = from.HasValue ? TimeZoneInfo.ConvertTimeToUtc(from.Value).ToString("O") : "";
        var toStr = to.HasValue ? TimeZoneInfo.ConvertTimeToUtc(to.Value).ToString("O") : "";
        return $"{fromStr}..{toStr}";
    }
}


public sealed class getInventoryTaskRequest
{

    public string FeedType { get; set; } = "LMS_ACTIVE_INVENTORY_REPORT";
    public string? ScheduledId { get; set; }
    public int? LookBackDays { get; set; }
    public DateRange? DateRange { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }

}
