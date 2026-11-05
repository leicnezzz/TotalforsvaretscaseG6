namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public class MatchSuggestionViewModel
{
    public int NeedId { get; set; }

    public string NeedName { get; set; } = "";

    public int ResourceId { get; set; }

    public string ResourceName { get; set; } = "";

    public double? DistanceKm { get; set; }
}