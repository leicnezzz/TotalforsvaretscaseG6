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
    int Id, string Name, CrisisType Type, string Area, CrisisSeverity Severity,
    DateTime StartTime, CrisisStatus Status, string Description, decimal? Latitude, decimal? Longitude);

public record OverviewNeed(
    int Id, string Name, string Type, string Priority, NeedStatus Status,
    int? CrisisId, string CrisisName, decimal? Latitude, decimal? Longitude);
