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

    public static IQueryable<ResourceAssignment> WhereResourceIsBusy(
        this IQueryable<ResourceAssignment> assignments) =>
        assignments.Where(assignment => assignment.Status == AssignmentStatus.Assigned ||
            (assignment.Status == AssignmentStatus.Requested &&
             ActiveNeedStatuses.Contains(assignment.Need.Status)));
}
