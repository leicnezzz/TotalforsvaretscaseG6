using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models
{
    public abstract class Bruker : IdentityUser<int> // Superklasse for brukere offentlig og privat
    {
        // Id (int) arves fra IdentityUser
        // UserName (string) arves fra IdentityUser
        // Email (string) arves fra IdentityUser
        // PhoneNumber (string) arves fra IdentityUser
        // PasswordHash (string) arves fra IdentityUser
        [Required]
        public string Navn { get; set; } = "";
    }
}
