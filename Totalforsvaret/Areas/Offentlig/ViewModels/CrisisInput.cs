using System.ComponentModel.DataAnnotations;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

/// <summary>
/// Validates crisis form fields and allows blank coordinates when editing.
/// </summary>
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
    [AllowedValues(CrisisType.Flood, CrisisType.Fire, CrisisType.PowerOutage, CrisisType.ExtremeWeather, CrisisType.Other,
        ErrorMessage = "Velg en gyldig krisetype.")]
    public CrisisType? Type { get; set; }

    [Required(ErrorMessage = "Velg en gyldig alvorlighetsgrad.")]
    [AllowedValues(CrisisSeverity.Low, CrisisSeverity.Medium, CrisisSeverity.High, CrisisSeverity.Critical,
        ErrorMessage = "Velg en gyldig alvorlighetsgrad.")]
    public CrisisSeverity? Severity { get; set; }

    [Required(ErrorMessage = "Velg en gyldig status.")]
    [AllowedValues(CrisisStatus.Active, CrisisStatus.UnderControl, CrisisStatus.Closed, ErrorMessage = "Velg en gyldig status.")]
    public CrisisStatus? Status { get; set; }

    [Required(ErrorMessage = "Velg et starttidspunkt.")]
    public DateTime? StartTime { get; set; }

    [Range(-Coordinate.LatitudeLimit * 1d, Coordinate.LatitudeLimit * 1d, ErrorMessage = "Ugyldig breddegrad.")]
    public virtual double? Latitude { get; set; }

    [Range(-Coordinate.LongitudeLimit * 1d, Coordinate.LongitudeLimit * 1d, ErrorMessage = "Ugyldig lengdegrad.")]
    public virtual double? Longitude { get; set; }

    /// <summary>Rejects the default date value as a crisis start time.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime == DateTime.MinValue)
            yield return new ValidationResult("Velg et starttidspunkt.", [nameof(StartTime)]);
    }
}

/// <summary>
/// Validates a new crisis and requires both coordinates.
/// </summary>
public sealed class CreateCrisisInput : CrisisInput
{
    [Required(ErrorMessage = "Velg en gyldig plassering på kartet.")]
    public override double? Latitude { get; set; }

    [Required(ErrorMessage = "Velg en gyldig plassering på kartet.")]
    public override double? Longitude { get; set; }
}
