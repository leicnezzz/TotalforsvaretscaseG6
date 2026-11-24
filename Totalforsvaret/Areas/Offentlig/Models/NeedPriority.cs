namespace Totalforsvaret.Areas.Offentlig.Models;

public enum NeedPriority
{
    Unspecified = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public static class NeedPriorityText
{
    private const string Low = "Lav";
    private const string Medium = "Middels";
    private const string High = "Høy";
    private const string Critical = "Kritisk";

    public static string ToNorwegianText(this NeedPriority priority) => priority switch
    {
        NeedPriority.Unspecified => "",
        NeedPriority.Low => Low,
        NeedPriority.Medium => Medium,
        NeedPriority.High => High,
        NeedPriority.Critical => Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown need priority.")
    };

    public static NeedPriority FromStoredValue(string value) => value switch
    {
        "" => NeedPriority.Unspecified,
        Low => NeedPriority.Low,
        Medium => NeedPriority.Medium,
        High => NeedPriority.High,
        Critical => NeedPriority.Critical,
        _ => throw new ArgumentException($"Unknown stored need priority: '{value}'.", nameof(value))
    };
}
