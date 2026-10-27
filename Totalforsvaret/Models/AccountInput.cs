using System.ComponentModel.DataAnnotations;
namespace Totalforsvaret.Models;
public class RegisterInput {
 [Required, MaxLength(100)] public string FirstName { get; set; } = "";
 [Required, MaxLength(100)] public string LastName { get; set; } = "";
 [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
 [Required, MinLength(12), RegularExpression(@"(?s)(?=.*\p{L})(?=.*\d).+")] public string Password { get; set; } = "";
 [Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}
public class LoginInput {
 [Required, EmailAddress] public string Email { get; set; } = "";
 [Required] public string Password { get; set; } = "";
 public string Actor { get; set; } = "civil";
 public bool RememberMe { get; set; }
}
public record LocalUser(string Id, string FirstName, string LastName, string Email, string PasswordHash);
