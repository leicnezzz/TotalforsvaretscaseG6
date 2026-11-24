using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

public interface IPublicOverviewRepository
{
    int CountActiveNeeds();
    int CountResolvedNeeds();
    int CountActiveCrises();
    List<Need> ListActiveNeedsOnMap();
    List<Need> ListLatestActiveNeeds();
    List<Need> ListActiveNeeds();
    List<CrisisMapAnnotation> ListAnnotations();
    List<ResourceAssignment> ListActiveAssignments();
    List<ResourceAssignment> ListClosedReviewsForActiveNeeds();
    List<OverviewCrisis> ListCrises();
    List<OverviewNeed> ListNeeds();
}

public record OverviewCrisis(
    int Id, string Name, string Type, string Area, string Severity,
    DateTime StartTime, string Status, string Description, double? Latitude, double? Longitude);

public record OverviewNeed(
    int Id, string Name, string Type, string Priority, string Status,
    int? CrisisId, string CrisisName, double? Latitude, double? Longitude);
