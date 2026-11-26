using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.Models.Inbox;

/// <summary>
/// Identifies which inbox visibility rules apply to the viewer.
/// </summary>
public enum InboxAudience
{
    Private,
    Public
}

/// <summary>
/// Carries the visible assignments and the conversation for the selected thread.
/// </summary>
public class InboxViewModel
{
    public InboxAudience Audience { get; init; }

    public IReadOnlyList<InboxItemViewModel> Items { get; init; }
        = [];

    public int? SelectedId { get; init; }

    public InboxItemViewModel? SelectedItem =>
        Items.FirstOrDefault(item => item.AssignmentId == SelectedId);

    public IReadOnlyList<InboxMessageViewModel> Messages { get; init; }
        = [];
}

/// <summary>
/// Combines assignment status with the resource and need details shown in an inbox thread.
/// </summary>
public class InboxItemViewModel
{
    public int AssignmentId { get; init; }
    public int ResourceId { get; init; }

    /// <summary>
    /// Indicates whether the viewer owns the resource and can manage the assignment from the inbox.
    /// </summary>
    public bool CanManageAssignment { get; init; }

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

    public AssignmentStatus Status { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }

    public bool IsVehicle => CategoryName == "Vehicles";

    public string CategoryLabel => IsVehicle ? "Kjøretøy" : "Utstyr";

    public string ContactLine => string.Join(
        " · ",
        new[] { NeedContactPerson, NeedContactPoint, NeedPhoneNumber }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

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

/// <summary>
/// Carries the sender, text and timestamp displayed in an inbox conversation.
/// </summary>
public class InboxMessageViewModel
{
    public string SenderName { get; init; } = "";
    public string Body { get; init; } = "";
    public DateTime SentAtUtc { get; init; }
    public bool IsOwnMessage { get; init; }
}