using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models
{
    /// <summary>
    /// Provides the shared Identity account fields and display name for both actor types.
    /// </summary>
    public abstract class Bruker : IdentityUser<int>
    {
        [Required]
        public string Navn { get; set; } = "";
    }
}
