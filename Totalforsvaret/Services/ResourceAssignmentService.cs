using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;

namespace Totalforsvaret.Services;

public enum AssignmentActionResult
{
    Succeeded,
    NotFound,
    NotRequested,
    NotAssigned,
    InactiveNeed,
    UnavailableResource,
    BusyResource
}

/// <summary>
/// Applies status rules to assignments that the caller is allowed to manage.
/// </summary>
public sealed class ResourceAssignmentService(IResourceAssignmentRepository assignments)
{
    /// <summary>
    /// Assigns a requested resource when the need is active, the resource is available and no
    /// other active assignment uses it.
    /// </summary>
    public Task<AssignmentActionResult> AcceptAsync(ResourceAssignment? assignment) =>
        assignment is null
            ? Task.FromResult(AssignmentActionResult.NotFound)
            : assignments.WithResourceLockAsync(assignment, AcceptLockedAsync);

    private async Task<AssignmentActionResult> AcceptLockedAsync(ResourceAssignment? assignment)
    {
        if (assignment is null)
            return AssignmentActionResult.NotFound;

        if (assignment.Status != AssignmentStatus.Requested)
            return AssignmentActionResult.NotRequested;

        var activeNeed = assignment.Need.Status is NeedStatus.Open or NeedStatus.InProgress;
        if (!activeNeed)
            return AssignmentActionResult.InactiveNeed;

        var available = assignment.Resource.Available &&
            (assignment.Resource.TilgjengeligFra == null ||
             assignment.Resource.TilgjengeligFra <= DateTime.Now);
        if (!available)
            return AssignmentActionResult.UnavailableResource;

        if (await assignments.HasOtherActiveAssignmentAsync(assignment.Id, assignment.ResourceId))
            return AssignmentActionResult.BusyResource;

        assignment.MarkAssigned();
        return await assignments.TrySaveStatusChangeAsync()
            ? AssignmentActionResult.Succeeded
            : AssignmentActionResult.NotRequested;
    }

    /// <summary>
    /// Rejects a pending assignment and records when it was closed.
    /// </summary>
    public async Task<AssignmentActionResult> DeclineAsync(ResourceAssignment? assignment)
    {
        if (assignment is null)
            return AssignmentActionResult.NotFound;

        if (assignment.Status != AssignmentStatus.Requested)
            return AssignmentActionResult.NotRequested;

        assignment.MarkRejected();
        return await assignments.TrySaveStatusChangeAsync()
            ? AssignmentActionResult.Succeeded
            : AssignmentActionResult.NotRequested;
    }

    /// <summary>
    /// Releases an assigned resource and retains the closed assignment in the history.
    /// </summary>
    public async Task<AssignmentActionResult> ReleaseAsync(ResourceAssignment? assignment)
    {
        if (assignment is null)
            return AssignmentActionResult.NotFound;

        if (assignment.Status != AssignmentStatus.Assigned)
            return AssignmentActionResult.NotAssigned;

        assignment.MarkReleased();
        return await assignments.TrySaveStatusChangeAsync()
            ? AssignmentActionResult.Succeeded
            : AssignmentActionResult.NotAssigned;
    }
}
