using System.ComponentModel.DataAnnotations;
using Totalforsvaret.Models;

namespace Totalforsvaret.Models.resourcemodels;

public class Resource
{
    public int ResourceId { get; set; }

    // Fremmednøkkel til AspNetUsers.Id.
    public int BrukerId { get; set; }

    public Bruker Bruker { get; set; } = null!;

    [MaxLength(100)]
    public required string Object { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public bool Available { get; set; }

    [MaxLength(200)]
    public string Navn { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Kontaktpunkt { get; set; } = string.Empty;

    public DateTime? TilgjengeligFra { get; set; }

    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    [MaxLength(100)]
    public string Condition { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AdditionalInformation { get; set; } = string.Empty;

    public VehicleDetails? Vehicle { get; set; }

    public EquipmentDetails? Equipment { get; set; }
}