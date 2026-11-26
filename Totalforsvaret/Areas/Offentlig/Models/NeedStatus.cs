namespace Totalforsvaret.Areas.Offentlig.Models;

public enum NeedStatus
{
    Unspecified = 0,
    Open = 1,
    InProgress = 2,
    Resolved = 3
}

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

    public static NeedStatus FromStoredValue(string value) => value switch
    {
        "" => NeedStatus.Unspecified,
        Open => NeedStatus.Open,
        InProgress => NeedStatus.InProgress,
        Resolved => NeedStatus.Resolved,
        _ => throw new ArgumentException($"Unknown stored need status: '{value}'.", nameof(value))
    };
}
