using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models.resourcemodels;

/// <summary>Stores the type and quantity associated with an equipment resource.</summary>
public class EquipmentDetails
{
    public int ResourceId { get; set; }

    public Resource Resource { get; set; } = null!;

    [MaxLength(100)]
    public string EquipmentType { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int Quantity { get; set; }
}