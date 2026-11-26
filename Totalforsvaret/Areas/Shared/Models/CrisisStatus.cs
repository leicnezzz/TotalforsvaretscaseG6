namespace Totalforsvaret.Models;

/// <summary>Represents the operational state of a crisis.</summary>
public enum CrisisStatus
{
    Unspecified = 0,
    Active = 1,
    UnderControl = 2,
    Closed = 3
}

/// <summary>Maps crisis statuses to the existing Norwegian values used for storage and display.</summary>
public static class CrisisStatusText
{
    private const string Active = "Aktiv";
    private const string UnderControl = "Under kontroll";
    private const string Closed = "Avsluttet";

    public static string ToNorwegianText(this CrisisStatus status) => status switch
    {
        CrisisStatus.Unspecified => "",
        CrisisStatus.Active => Active,
        CrisisStatus.UnderControl => UnderControl,
        CrisisStatus.Closed => Closed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown crisis status.")
    };

    /// <summary>Reads the existing Norwegian database value. Empty values map to Unspecified and unknown values are rejected.</summary>
    public static CrisisStatus FromStoredValue(string value) => value switch
    {
        "" => CrisisStatus.Unspecified,
        Active => CrisisStatus.Active,
        UnderControl => CrisisStatus.UnderControl,
        Closed => CrisisStatus.Closed,
        _ => throw new ArgumentException($"Unknown stored crisis status: '{value}'.", nameof(value))
    };
}
