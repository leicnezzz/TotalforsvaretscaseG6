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

public sealed class ResourceAssignmentService(IResourceAssignmentRepository assignments)
{
    public async Task<AssignmentActionResult> AcceptAsync(ResourceAssignment? assignment)
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
        await assignments.SaveChangesAsync();
        return AssignmentActionResult.Succeeded;
    }

    public async Task<AssignmentActionResult> DeclineAsync(ResourceAssignment? assignment)
    {
        if (assignment is null)
            return AssignmentActionResult.NotFound;

        if (assignment.Status != AssignmentStatus.Requested)
            return AssignmentActionResult.NotRequested;

        assignment.MarkRejected();
        await assignments.SaveChangesAsync();
        return AssignmentActionResult.Succeeded;
    }

    public async Task<AssignmentActionResult> ReleaseAsync(ResourceAssignment? assignment)
    {
        if (assignment is null)
            return AssignmentActionResult.NotFound;

        if (assignment.Status != AssignmentStatus.Assigned)
            return AssignmentActionResult.NotAssigned;

        assignment.MarkReleased();
        await assignments.SaveChangesAsync();
        return AssignmentActionResult.Succeeded;
    }
}
