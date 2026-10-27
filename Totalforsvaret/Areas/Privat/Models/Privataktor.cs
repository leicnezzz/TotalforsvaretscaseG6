using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Privat.Models
{
    public class Privataktor : Bruker // Arver identity fra Bruker-klassen
    {
        // Automatisk arvet fra IdentityUser<int>:
        // Id (int) - BrukerID (og identifikator for Offentligaktor)
        // UserName (string) - Brukernavn
        // Email (string) - E-postadresse
        // PhoneNumber (string) - Telefonnummer
        // PasswordHash (string) - Hashet passord

        // Automatisk arvet fra Bruker:
        // Navn (string) - Brukerens navn

        // Egenskaper spesifikke for Offentligaktor:
        public string? Organisasjonnummer { get; set; }
    }
}
