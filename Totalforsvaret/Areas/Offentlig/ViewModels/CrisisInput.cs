using System.ComponentModel.DataAnnotations;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public class CrisisInput : IValidatableObject
{
    [Required(ErrorMessage = "Navnet må fylles ut og kan ha maksimalt 100 tegn.")]
    [StringLength(100, ErrorMessage = "Navnet må fylles ut og kan ha maksimalt 100 tegn.")]
    public string Name { get => field; set => field = value?.Trim() ?? ""; } = "";

    [Required(ErrorMessage = "Beskrivelsen må fylles ut og kan ha maksimalt 1500 tegn.")]
    [StringLength(1500, ErrorMessage = "Beskrivelsen må fylles ut og kan ha maksimalt 1500 tegn.")]
    public string Description { get => field; set => field = value?.Trim() ?? ""; } = "";

    [StringLength(100, ErrorMessage = "Områdedetaljene kan ha maksimalt 100 tegn.")]
    public string? Area { get => field; set => field = value?.Trim(); }

    [Required(ErrorMessage = "Velg en gyldig krisetype.")]
    [AllowedValues("Flom", "Brann", "Strømbrudd", "Ekstremvær", "Annet", ErrorMessage = "Velg en gyldig krisetype.")]
    public string Type { get => field; set => field = value?.Trim() ?? ""; } = "";

    [Required(ErrorMessage = "Velg en gyldig alvorlighetsgrad.")]
    [AllowedValues("Lav", "Middels", "Høy", "Kritisk", ErrorMessage = "Velg en gyldig alvorlighetsgrad.")]
    public string Severity { get => field; set => field = value?.Trim() ?? ""; } = "";

    [Required(ErrorMessage = "Velg en gyldig status.")]
    [AllowedValues(CrisisStatus.Active, CrisisStatus.UnderControl, CrisisStatus.Closed, ErrorMessage = "Velg en gyldig status.")]
    public CrisisStatus? Status { get; set; }

    [Required(ErrorMessage = "Velg et starttidspunkt.")]
    public DateTime? StartTime { get; set; }

    [Range(-90d, 90d, ErrorMessage = "Ugyldig breddegrad.")]
    public virtual double? Latitude { get; set; }

    [Range(-180d, 180d, ErrorMessage = "Ugyldig lengdegrad.")]
    public virtual double? Longitude { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime == DateTime.MinValue)
            yield return new ValidationResult("Velg et starttidspunkt.", [nameof(StartTime)]);
    }
}

public sealed class CreateCrisisInput : CrisisInput
{
    [Required(ErrorMessage = "Velg en gyldig plassering på kartet.")]
    public override double? Latitude { get; set; }

    [Required(ErrorMessage = "Velg en gyldig plassering på kartet.")]
    public override double? Longitude { get; set; }
}
