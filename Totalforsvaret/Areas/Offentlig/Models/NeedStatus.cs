namespace Totalforsvaret.Areas.Offentlig.Models;

/// <summary>Represents the workflow state of a registered need.</summary>
public enum NeedStatus
{
    Unspecified = 0,
    Open = 1,
    InProgress = 2,
    Resolved = 3
}

/// <summary>Maps need statuses to the existing Norwegian values used for storage and display.</summary>
public static class NeedStatusText
{
    private const string Open = "Åpen";
    private const string InProgress = "Under behandling";
    private const string Resolved = "Løst";

    public static string ToNorwegianText(this NeedStatus status) => status switch
    {
        NeedStatus.Unspecified => "",
        NeedStatus.Open => Open,
        NeedStatus.InProgress => InProgress,
        NeedStatus.Resolved => Resolved,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown need status.")
    };

    /// <summary>Reads the existing Norwegian database value. Empty values map to Unspecified and unknown values are rejected.</summary>
    public static NeedStatus FromStoredValue(string value) => value switch
    {
        "" => NeedStatus.Unspecified,
        Open => NeedStatus.Open,
        InProgress => NeedStatus.InProgress,
        Resolved => NeedStatus.Resolved,
        _ => throw new ArgumentException($"Unknown stored need status: '{value}'.", nameof(value))
    };
}
