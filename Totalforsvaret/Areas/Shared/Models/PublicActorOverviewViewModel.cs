using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Models;

/// <summary>
/// Contains the counters, map data and match suggestions used by the public actor overview.
/// </summary>
public sealed class PublicActorOverviewViewModel
{
    public required int CurrentUserId { get; init; }
    public required int ActiveNeedCount { get; init; }
    public required IReadOnlyList<Resource> AvailableResources { get; init; }
    public required IReadOnlyList<Need> MapNeeds { get; init; }
    public required IReadOnlyList<CrisisMapAnnotation> MapAnnotations { get; init; }
    public required IReadOnlyList<OverviewCrisis> Crises { get; init; }
    public required IReadOnlyList<OverviewNeed> Needs { get; init; }
    public required IReadOnlyList<ResourceAssignment> ActiveAssignments { get; init; }
    public required IReadOnlySet<int> BusyResourceIds { get; init; }
    public required IReadOnlyDictionary<(int NeedId, int ResourceId), AssignmentStatus> MatchStatuses { get; init; }
    public required IReadOnlyList<MatchSuggestionViewModel> MatchSuggestions { get; init; }
}
