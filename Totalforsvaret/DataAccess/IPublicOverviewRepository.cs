using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

/// <summary>
/// Provides read data for the public actor map, lists and matching workflow.
/// </summary>
public interface IPublicOverviewRepository
{
    int CountActiveNeeds();

    /// <summary>
    /// Returns active needs with both coordinates populated.
    /// </summary>
    List<Need> ListActiveNeedsOnMap();
    List<Need> ListActiveNeeds();
    List<CrisisMapAnnotation> ListAnnotations();

    /// <summary>
    /// Returns requested and assigned matches that occupy resources on the overview.
    /// </summary>
    List<ResourceAssignment> ListActiveAssignments();

    /// <summary>
    /// Lists rejected and released matches for needs that are still active.
    /// </summary>
    List<ResourceAssignment> ListClosedReviewsForActiveNeeds();
    List<OverviewCrisis> ListCrises();
    List<OverviewNeed> ListNeeds();
}

public record OverviewCrisis(
    int Id, string Name, CrisisType Type, string Area, CrisisSeverity Severity,
    DateTime StartTime, CrisisStatus Status, string Description, decimal? Latitude, decimal? Longitude);

public record OverviewNeed(
    int Id, string Name, NeedType Type, NeedPriority Priority, NeedStatus Status,
    int? CrisisId, string CrisisName, decimal? Latitude, decimal? Longitude);
