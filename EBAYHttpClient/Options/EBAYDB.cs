namespace EBAYHttpClient.Options;

public sealed class EBAYDB
{
    public static string EBAYDBOptionsKey => "EBAYDB";

    public required string ConnectionString { get; set; }

    public int CommandTimeoutSeconds { get; set; } = 120;

    public string DefaultSource { get; set; } = "SellFeedApi";

    public string DefaultFeedType { get; set; } = "LMS_ACTIVE_INVENTORY_REPORT";
}
