using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

/// <summary>
/// Carries the editable personal and organizational details shown on the public actor profile.
/// </summary>
public class PublicActorProfileViewModel
{
    [Required(ErrorMessage = "Skriv inn fornavn.")]
    [Display(Name = "Fornavn")]
    public string Fornavn { get; set; } = "";

    [Required(ErrorMessage = "Skriv inn etternavn.")]
    [Display(Name = "Etternavn")]
    public string Etternavn { get; set; } = "";

    [Display(Name = "Avdeling")]
    public string? Avdeling { get; set; }

    [Display(Name = "E-postadresse")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Skriv inn et gyldig telefonnummer.")]
    [Display(Name = "Telefonnummer")]
    public string? PhoneNumber { get; set; }

    public bool HasPhoto { get; set; }

    public string Initialer =>
        $"{char.ToUpperInvariant(Fornavn.FirstOrDefault())}" +
        $"{char.ToUpperInvariant(Etternavn.FirstOrDefault())}".Trim();
}