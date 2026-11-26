namespace Totalforsvaret.Areas.Privat.Models;

/// <summary>
/// Records whether a private actor has completed a specific preparedness checklist item.
/// </summary>
public class EmergencyChecklistStatus
{
    public int BrukerId { get; set; }
    public string ItemId { get; set; } = "";
    public bool IsComplete { get; set; }
}