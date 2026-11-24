using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models
{
    public abstract class Bruker : IdentityUser<int>
    {
        [Required]
        public string Navn { get; set; } = "";
    }
}
