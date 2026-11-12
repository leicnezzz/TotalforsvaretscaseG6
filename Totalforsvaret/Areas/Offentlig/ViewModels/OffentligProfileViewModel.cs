using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public class OffentligProfileViewModel
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
        $"{(Fornavn.FirstOrDefault() is char first ? char.ToUpperInvariant(first) : ' ')}" +
        $"{(Etternavn.FirstOrDefault() is char last ? char.ToUpperInvariant(last) : ' ')}".Trim();
}