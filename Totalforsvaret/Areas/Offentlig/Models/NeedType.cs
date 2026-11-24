namespace Totalforsvaret.Areas.Offentlig.Models;

public enum NeedType
{
    Unspecified = 0,
    Transport = 1,
    Equipment = 2
}

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

    public static NeedType FromStoredValue(string value) => value switch
    {
        "" => NeedType.Unspecified,
        Transport => NeedType.Transport,
        Equipment => NeedType.Equipment,
        _ => throw new ArgumentException($"Unknown stored need type: '{value}'.", nameof(value))
    };
}
