namespace Totalforsvaret.Areas.Privat.Models;

public class EgenberedskapChecklistStatus
{
    public int BrukerId { get; set; }
    public string ItemId { get; set; } = "";
    public bool IsComplete { get; set; }
}