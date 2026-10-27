using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Models
{
    public class Offentligaktør : Bruker
    {
        // Arves fra IdentityUser<int>:
        // Id, UserName, Email, PhoneNumber, PasswordHash

        // Arves fra Bruker:
        // Navn

        public string? Avdeling { get; set; }
    }
}
