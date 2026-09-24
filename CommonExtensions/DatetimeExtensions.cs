using System.Globalization;

namespace EBAY.Shared;

public static class DateTimeExtensions
{

    private static readonly TimeZoneInfo BerlinTimeZone = GetBerlinTimeZone();
    private static string datetimemask = "yyyy-MM-ddTHH:mm:ss.fff'Z'";

    private static TimeZoneInfo GetBerlinTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
    }

    /// <summary>
    /// Toma la fecha en local y te devuelve un string en UTC listo para usar en el API.
    /// </summary>
    /// <param name="localtime"></param>
    /// <returns></returns>
    public static string ToUTCJsonString(this DateTime localtime)
    {
        var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localtime, DateTimeKind.Unspecified), BerlinTimeZone);
        return utc.ToString(datetimemask, CultureInfo.InvariantCulture);

    }

    public static DateTime NowLocal() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BerlinTimeZone).DateTime;
}