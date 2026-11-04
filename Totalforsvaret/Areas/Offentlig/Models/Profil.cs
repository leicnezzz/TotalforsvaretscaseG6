using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Areas.Privat.Models;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Skriv inn navnet ditt.")]
    [Display(Name = "Navn")]
    public string Navn { get; set; } = "";

    [Display(Name = "E-postadresse")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Skriv inn et gyldig telefonnummer.")]
    [Display(Name = "Telefonnummer")]
    public string? PhoneNumber { get; set; }
}