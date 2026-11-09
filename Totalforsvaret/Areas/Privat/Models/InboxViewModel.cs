namespace Totalforsvaret.Areas.Privat.Models;

using Totalforsvaret.Areas.Offentlig.Models;

public class InboxViewModel
{
    public IReadOnlyList<InboxItemViewModel> Items { get; init; }
        = Array.Empty<InboxItemViewModel>();

    public int? SelectedId { get; init; }

    public InboxItemViewModel? SelectedItem =>
        Items.FirstOrDefault(item => item.AssignmentId == SelectedId);
}

public class InboxItemViewModel
{
    public int AssignmentId { get; init; }
    public int ResourceId { get; init; }

    public string ResourceName { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public bool ResourceAvailable { get; init; }
    public string ResourceAddress { get; init; } = "";
    public string ResourceCondition { get; init; } = "";
    public string ResourceInformation { get; init; } = "";

    public string NeedName { get; init; } = "";
    public string NeedType { get; init; } = "";
    public string NeedPriority { get; init; } = "";
    public string NeedStatus { get; init; } = "";
    public string NeedDescription { get; init; } = "";
    public string? NeedContactPerson { get; init; }
    public string? NeedContactPoint { get; init; }
    public string? NeedPhoneNumber { get; init; }
    public string? NeedAddress { get; init; }
    public DateTime? NeedDeadline { get; init; }

    public AssignmentStatus Status { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }

    public string StatusLabel => Status switch
    {
        AssignmentStatus.Requested => "Venter på svar",
        AssignmentStatus.Assigned => "Tildelt",
        AssignmentStatus.Rejected => "Avslått",
        AssignmentStatus.Released => "Frigitt",
        AssignmentStatus.Cancelled => "Avbrutt",
        _ => "Ukjent status"
    };
}