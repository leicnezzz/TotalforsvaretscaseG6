namespace Totalforsvaret.Models;

public enum CrisisType
{
    Unspecified = 0,
    Flood = 1,
    Fire = 2,
    PowerOutage = 3,
    ExtremeWeather = 4,
    Other = 5
}

public static class CrisisTypeText
{
    private const string Flood = "Flom";
    private const string Fire = "Brann";
    private const string PowerOutage = "Strømbrudd";
    private const string ExtremeWeather = "Ekstremvær";
    private const string Other = "Annet";

    public static string ToNorwegianText(this CrisisType type) => type switch
    {
        CrisisType.Unspecified => "",
        CrisisType.Flood => Flood,
        CrisisType.Fire => Fire,
        CrisisType.PowerOutage => PowerOutage,
        CrisisType.ExtremeWeather => ExtremeWeather,
        CrisisType.Other => Other,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown crisis type.")
    };

    public static CrisisType FromStoredValue(string value) => value switch
    {
        "" => CrisisType.Unspecified,
        Flood => CrisisType.Flood,
        Fire => CrisisType.Fire,
        PowerOutage => CrisisType.PowerOutage,
        ExtremeWeather => CrisisType.ExtremeWeather,
        Other => CrisisType.Other,
        _ => throw new ArgumentException($"Unknown stored crisis type: '{value}'.", nameof(value))
    };
}
