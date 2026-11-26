namespace Totalforsvaret.Areas.Offentlig.Models;

/// <summary>
/// Identifies whether a need calls for transport or equipment.
/// </summary>
public enum NeedType
{
    Unspecified = 0,
    Transport = 1,
    Equipment = 2
}

/// <summary>
/// Maps need types to the existing Norwegian values used for storage and display.
/// </summary>
public static class NeedTypeText
{
    private const string Transport = "Transport";
    private const string Equipment = "Utstyr";

    public static string ToNorwegianText(this NeedType type) => type switch
    {
        NeedType.Unspecified => "",
        NeedType.Transport => Transport,
        NeedType.Equipment => Equipment,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown need type.")
    };

    /// <summary>Reads the existing Norwegian database value. Empty values map to Unspecified and unknown values are rejected.</summary>
    public static NeedType FromStoredValue(string value) => value switch
    {
        "" => NeedType.Unspecified,
        Transport => NeedType.Transport,
        Equipment => NeedType.Equipment,
        _ => throw new ArgumentException($"Unknown stored need type: '{value}'.", nameof(value))
    };
}
