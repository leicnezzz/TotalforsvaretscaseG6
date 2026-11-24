using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Services;

public sealed class ResourceMatchingService
{
    public List<MatchSuggestionViewModel> FindSuggestions(
        IEnumerable<Need> needs,
        IEnumerable<Resource> availableResources,
        IReadOnlyDictionary<(int NeedId, int ResourceId), AssignmentStatus> statuses)
    {
        var now = DateTime.Now;
        var candidates = availableResources
            .Where(resource => resource.TilgjengeligFra == null || resource.TilgjengeligFra <= now)
            .ToList();
        var suggestions = new List<MatchSuggestionViewModel>();

        foreach (var need in needs)
        {
            var category = need.Type switch
            {
                "Transport" => "Vehicles",
                "Utstyr" => "Equipment",
                _ => null
            };

            if (category == null)
                continue;

            foreach (var resource in candidates.Where(resource => resource.Category.Name == category))
            {
                suggestions.Add(new MatchSuggestionViewModel
                {
                    NeedId = need.Id,
                    NeedName = need.Name,
                    ResourceId = resource.ResourceId,
                    ResourceName = resource.Object,
                    DistanceKm = BeregnAvstandKm(need.Latitude, need.Longitude, resource.Latitude, resource.Longitude)
                });
            }
        }

        return suggestions
            .OrderBy(suggestion =>
            {
                if (!statuses.TryGetValue((suggestion.NeedId, suggestion.ResourceId), out var status))
                    return 0;

                return status == AssignmentStatus.Requested ? 1 : 2;
            })
            .ThenBy(suggestion => suggestion.DistanceKm ?? double.MaxValue)
            .ThenBy(suggestion => suggestion.NeedId)
            .ThenBy(suggestion => suggestion.ResourceId)
            .ToList();
    }

    private static double? BeregnAvstandKm(
        decimal? behovLatitude,
        decimal? behovLongitude,
        decimal ressursLatitude,
        decimal ressursLongitude)
    {
        if (!behovLatitude.HasValue || !behovLongitude.HasValue)
            return null;
        
        var lat1 = (double)behovLatitude.Value;
        var lon1 = (double)behovLongitude.Value;
        var lat2 = (double)ressursLatitude;
        var lon2 = (double)ressursLongitude;

        if (!double.IsFinite(lat1) || !double.IsFinite(lon1) ||
            lat1 < -90 || lat1 > 90 ||
            lon1 < -180 || lon1 > 180 ||
            lat2 < -90 || lat2 > 90 ||
            lon2 < -180 || lon2 > 180)
            return null;

        const double jordradiusKm = 6371;
        const double radianer = Math.PI / 180;

        var deltaLat = (lat2 - lat1) * radianer;
        var deltaLon = (lon2 - lon1) * radianer;

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                Math.Cos(lat1 * radianer) *
                Math.Cos(lat2 * radianer) *
                Math.Pow(Math.Sin(deltaLon / 2), 2);

        return jordradiusKm * 2 *
               Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
