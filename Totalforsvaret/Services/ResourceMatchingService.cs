using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Services;

/// <summary>
/// Builds and ranks resource suggestions for active needs.
/// </summary>
public sealed class ResourceMatchingService
{
    /// <summary>
    /// Matches resources by category. Ranks unreviewed suggestions first, followed by requested
    /// and other reviewed matches, then sorts by distance within each group.
    /// </summary>
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
                NeedType.Transport => "Vehicles",
                NeedType.Equipment => "Equipment",
                _ => null
            };

            if (category == null)
                continue;

            var hasNeedLocation = Coordinate.TryCreate(need.Latitude, need.Longitude, out var needLocation);

            foreach (var resource in candidates.Where(resource => resource.Category.Name == category))
            {
                suggestions.Add(new MatchSuggestionViewModel
                {
                    NeedId = need.Id,
                    NeedName = need.Name,
                    ResourceId = resource.ResourceId,
                    ResourceName = resource.Object,
                    DistanceKm = hasNeedLocation && Coordinate.TryCreate(resource.Latitude, resource.Longitude, out var resourceLocation)
                        ? CalculateDistanceKm(needLocation, resourceLocation)
                        : null
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

    private static double CalculateDistanceKm(Coordinate from, Coordinate to)
    {
        var lat1 = (double)from.Latitude;
        var lon1 = (double)from.Longitude;
        var lat2 = (double)to.Latitude;
        var lon2 = (double)to.Longitude;

        const double earthRadiusKm = 6371;
        const double radiansPerDegree = Math.PI / 180;

        var deltaLat = (lat2 - lat1) * radiansPerDegree;
        var deltaLon = (lon2 - lon1) * radiansPerDegree;

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                Math.Cos(lat1 * radiansPerDegree) *
                Math.Cos(lat2 * radiansPerDegree) *
                Math.Pow(Math.Sin(deltaLon / 2), 2);

        return earthRadiusKm * 2 *
               Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
