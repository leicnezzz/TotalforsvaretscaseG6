using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Totalforsvaret.Models;

public class FormDataModel : IValidatableObject
{
    [Required(ErrorMessage = "Skriv inn navn."), StringLength(200)]
    public string Navn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Velg eller skriv inn ressurstype."), StringLength(100)]
    public string RessursType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Velg kategori.")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Velg om ressursen er tilgjengelig.")]
    public bool? Available { get; set; }

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> Categories { get; set; } = [];

    [Required(ErrorMessage = "Velg tidspunkt.")]
    public DateTime? TilgjengeligFra { get; set; }

    [Required(ErrorMessage = "Skriv inn telefon eller e-post."), StringLength(200)]
    public string Kontaktpunkt { get; set; } = string.Empty;

    // Kartet sender koordinater med punktum uavhengig av nettleserens språk.
    [Required(ErrorMessage = "Velg en posisjon i kartet.")]
    public string Latitude { get; set; } = string.Empty;

    [Required(ErrorMessage = "Velg en posisjon i kartet.")]
    public string Longitude { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidCoordinate(Latitude, 90))
            yield return new ValidationResult("Velg en gyldig breddegrad i kartet.", [nameof(Latitude)]);
        if (!ValidCoordinate(Longitude, 180))
            yield return new ValidationResult("Velg en gyldig lengdegrad i kartet.", [nameof(Longitude)]);
    }

    private static bool ValidCoordinate(string value, decimal limit) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var coordinate)
        && coordinate >= -limit && coordinate <= limit;
}
