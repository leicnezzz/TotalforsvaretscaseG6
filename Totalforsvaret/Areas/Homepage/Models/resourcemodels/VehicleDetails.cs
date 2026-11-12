using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models.resourcemodels;

public class VehicleDetails
{
    public int ResourceId { get; set; }

    public Resource Resource { get; set; } = null!;

    [MaxLength(12)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(100)]
    public string RequiredLicense { get; set; } = string.Empty;
}