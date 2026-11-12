using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models;

public class ResourceInput : IValidatableObject
{
    [Required, MaxLength(100)]
    public string Object { get; set; } = "";

    [Required, Range(1, 2)]
    public int? CategoryId { get; set; }

    [Required]
    public bool? Available { get; set; }

    [Required, MaxLength(25)]
    public string Kontaktpunkt { get; set; } = "";

    public DateTime? TilgjengeligFra { get; set; }

    [Required, MaxLength(250)]
    public string Address { get; set; } = "";

    [Required, Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }

    [Required, Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }

    [Required, MaxLength(100)]
    public string Condition { get; set; } = "";

    [MaxLength(200)]
    public string? AdditionalInformation { get; set; } = "";

    public VehicleInput? Vehicle { get; set; }

    public EquipmentInput? Equipment { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var phone = Kontaktpunkt ?? "";
        var digits = phone.Count(char.IsDigit);

        if (digits < 8 || digits > 15 ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                phone, @"^\+?[\d\s()-]+$"))
        {
            yield return new ValidationResult(
                "Oppgi et gyldig telefonnummer med 8–15 sifre.",
                [nameof(Kontaktpunkt)]);
        }

        if (CategoryId == 1 && (Vehicle is null || Equipment is not null))
        {
            yield return new ValidationResult(
                "Kjøretøy må ha kjøretøydetaljer.",
                [nameof(Vehicle)]);
        }

        if (CategoryId == 2 && (Equipment is null || Vehicle is not null))
        {
            yield return new ValidationResult(
                "Utstyr må ha utstyrsdetaljer.",
                [nameof(Equipment)]);
        }
    }
}

public class PublicResourceInput : ResourceInput
{
    [Required, MaxLength(99)]
    public string ContactFirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string ContactLastName { get; set; } = "";

    // Telefon (25) og skilletegn (3) lagres i samme felt på 200 tegn.
    [Required, EmailAddress, MaxLength(172)]
    public string ContactEmail { get; set; } = "";
}

public class VehicleInput
{
    [MaxLength(12)]
    [RegularExpression(@"^[\p{L}\p{N} -]{2,12}$")]
    public string? RegistrationNumber { get; set; }

    [MaxLength(100)]
    public string? RequiredLicense { get; set; } = "";
}

public class EquipmentInput
{
    [Required]
    [RegularExpression(
        "^(Drone|Verktøy|Aggregat|Sambandsutstyr|Annet utstyr)$")]
    public string EquipmentType { get; set; } = "";

    [Required, Range(1, 100000)]
    public int? Quantity { get; set; }
}
