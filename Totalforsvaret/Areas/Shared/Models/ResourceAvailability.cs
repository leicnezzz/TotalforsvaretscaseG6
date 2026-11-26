namespace Totalforsvaret.Models;

internal static class ResourceAvailability
{
    private static readonly TimeZoneInfo Norway = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Norway);
}
