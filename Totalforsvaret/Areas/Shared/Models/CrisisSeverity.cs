namespace Totalforsvaret.Models;

/// <summary>
/// Represents the severity levels used to rank crises and select alerts.
/// </summary>
public enum CrisisSeverity
{
    Unspecified = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Maps crisis severity to the existing Norwegian values used for storage and display.
/// </summary>
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

    /// <summary>
    /// Reads the existing Norwegian database value. Empty values map to Unspecified and unknown values are rejected.
    /// </summary>
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
