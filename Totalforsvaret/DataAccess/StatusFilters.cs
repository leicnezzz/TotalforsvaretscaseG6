using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.DataAccess;

/// <summary>
/// Defines the statuses shared by overview and assignment queries for active needs and
/// occupied resources.
/// </summary>
internal static class StatusFilters
{
    public static IReadOnlyList<NeedStatus> ActiveNeedStatuses { get; } =
        [NeedStatus.Open, NeedStatus.InProgress];

    public static IReadOnlyList<AssignmentStatus> BusyAssignmentStatuses { get; } =
        [AssignmentStatus.Requested, AssignmentStatus.Assigned];
}
