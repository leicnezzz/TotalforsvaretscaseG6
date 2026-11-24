using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.DataAccess;

internal static class StatusFilters
{
    public static IReadOnlyList<NeedStatus> ActiveNeedStatuses { get; } =
        [NeedStatus.Open, NeedStatus.InProgress];

    public static IReadOnlyList<AssignmentStatus> BusyAssignmentStatuses { get; } =
        [AssignmentStatus.Requested, AssignmentStatus.Assigned];
}
