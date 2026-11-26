using System.ComponentModel.DataAnnotations;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Models;

/// <summary>
/// Stores a message, its sender and its timestamp within a resource assignment thread.
/// </summary>
public class ResourceMessage
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }
    public ResourceAssignment Assignment { get; set; } = null!;

    public int SenderUserId { get; set; }
    public Bruker Sender { get; set; } = null!;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Body { get; set; } = "";

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}