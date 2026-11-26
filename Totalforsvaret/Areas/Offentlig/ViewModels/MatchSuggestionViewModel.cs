namespace Totalforsvaret.Areas.Offentlig.ViewModels;

/// <summary>
/// Represents a suggested match between a need and a resource, including distance when available.
/// </summary>
public class MatchSuggestionViewModel
{
    public int NeedId { get; set; }

    public string NeedName { get; set; } = "";

    public int ResourceId { get; set; }

    public string ResourceName { get; set; } = "";

    public double? DistanceKm { get; set; }
}