using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Areas.Offentlig.Models;

public enum AssignmentStatus
{
    Requested = 0,
    Assigned = 1,
    Rejected = 2,
    Released = 3,
    Cancelled = 4
}

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

    public DateTime? AssignedAtUtc { get; set; }

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
