using System.ComponentModel.DataAnnotations;
namespace Totalforsvaret.Models;
/// <summary>
/// Validates the contact details and password supplied for private account registration.
/// </summary>
public class RegisterInput {
 [Required, MaxLength(100)] public string FirstName { get; set; } = "";
 [Required, MaxLength(100)] public string LastName { get; set; } = "";
 [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
 [Required, MinLength(12), RegularExpression(@"(?s)(?=.*\p{L})(?=.*\d).+")] public string Password { get; set; } = "";
 [Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}

/// <summary>
/// Carries the credentials, actor selection and session preference submitted when signing in.
/// </summary>
public class LoginInput {
 [Required, EmailAddress] public string Email { get; set; } = "";
 [Required] public string Password { get; set; } = "";
 public string Actor { get; set; } = "civil";
 public bool RememberMe { get; set; }
}
