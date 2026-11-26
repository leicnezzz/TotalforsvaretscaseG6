using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Areas.Offentlig.Models;

/// <summary>
/// Represents the persisted lifecycle of a resource request and its eventual outcome.
/// </summary>
public enum AssignmentStatus
{
    Requested = 0,
    Assigned = 1,
    Rejected = 2,
    Released = 3,
    Cancelled = 4
}

/// <summary>
/// Links a resource to a need and records the status and timestamps of the assignment.
/// </summary>
public class ResourceAssignment
{
    public int Id { get; set; }

    public int NeedId { get; set; }

    public Need Need { get; set; } = null!;
    
    public int ResourceId { get; set; }

    public Resource Resource { get; set; } = null!;

    public AssignmentStatus Status { get; set; }
        = AssignmentStatus.Requested;

    public DateTime RequestedAtUtc { get; set; }
        = DateTime.UtcNow;

    /// <summary>
    /// Records when the resource was assigned. The timestamp remains available after the assignment closes.
    /// </summary>
    public DateTime? AssignedAtUtc { get; set; }

    /// <summary>
    /// Records when the assignment was closed, if a closing timestamp has been set.
    /// </summary>
    public DateTime? ClosedAtUtc { get; set; }
    
    public ICollection<ResourceMessage> Messages { get; set; }
        = new List<ResourceMessage>();

    internal void MarkAssigned()
    {
        Status = AssignmentStatus.Assigned;
        AssignedAtUtc = DateTime.UtcNow;
    }

    internal void MarkRejected() => Close(AssignmentStatus.Rejected);

    internal void MarkReleased() => Close(AssignmentStatus.Released);

    private void Close(AssignmentStatus status)
    {
        Status = status;
        ClosedAtUtc = DateTime.UtcNow;
    }
}
