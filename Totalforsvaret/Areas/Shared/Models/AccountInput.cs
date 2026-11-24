using System.ComponentModel.DataAnnotations;
namespace Totalforsvaret.Models;
// Registrering av Fornavn og etternavn, maks 100 tegn hver.
// Registrering av e-post (maks 254 tegn) med passord bekreftelse (passord må minst ha 12 tegn, minst en bokstav og siffer).
public class RegisterInput {
 [Required, MaxLength(100)] public string FirstName { get; set; } = "";
 [Required, MaxLength(100)] public string LastName { get; set; } = "";
 [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
 [Required, MinLength(12), RegularExpression(@"(?s)(?=.*\p{L})(?=.*\d).+")] public string Password { get; set; } = "";
 [Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}
// Informasjon for innlogging (påkrevd e-post og passord).
// Aktørtypoe "Civil" ble satt som standard og valg om å huske innloggingen.
public class LoginInput {
 [Required, EmailAddress] public string Email { get; set; } = "";
 [Required] public string Password { get; set; } = "";
 public string Actor { get; set; } = "civil";
 public bool RememberMe { get; set; }
}
