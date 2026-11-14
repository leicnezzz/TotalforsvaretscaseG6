using System.ComponentModel.DataAnnotations;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Models;

public class ResourceMessage
{
    public int Id { get; set; }

    // Matchen denne samtalen gjelder.
    public int AssignmentId { get; set; }
    public ResourceAssignment Assignment { get; set; } = null!;

    // Brukeren som sendte akkurat denne meldingen.
    public int SenderUserId { get; set; }
    public Bruker Sender { get; set; } = null!;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Body { get; set; } = "";

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}