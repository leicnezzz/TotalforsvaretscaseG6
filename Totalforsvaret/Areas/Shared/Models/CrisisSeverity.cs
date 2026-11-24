namespace Totalforsvaret.Models;

public enum CrisisSeverity
{
    Unspecified = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public static class CrisisSeverityText
{
    private const string Low = "Lav";
    private const string Medium = "Middels";
    private const string High = "Høy";
    private const string Critical = "Kritisk";

    public static string ToNorwegianText(this CrisisSeverity severity) => severity switch
    {
        CrisisSeverity.Unspecified => "",
        CrisisSeverity.Low => Low,
        CrisisSeverity.Medium => Medium,
        CrisisSeverity.High => High,
        CrisisSeverity.Critical => Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown crisis severity.")
    };

    public static CrisisSeverity FromStoredValue(string value) => value switch
    {
        "" => CrisisSeverity.Unspecified,
        Low => CrisisSeverity.Low,
        Medium => CrisisSeverity.Medium,
        High => CrisisSeverity.High,
        Critical => CrisisSeverity.Critical,
        _ => throw new ArgumentException($"Unknown stored crisis severity: '{value}'.", nameof(value))
    };
}
