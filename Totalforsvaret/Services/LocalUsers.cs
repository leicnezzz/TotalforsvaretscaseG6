using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Totalforsvaret.Models;
namespace Totalforsvaret.Services;
public class LocalUsers(IWebHostEnvironment environment) {
 private readonly string path = Path.Combine(environment.ContentRootPath, "App_Data", "users.json");
 private readonly object gate = new();
 private readonly PasswordHasher<LocalUser> hasher = new();
 private List<LocalUser> Read() => File.Exists(path) ? JsonSerializer.Deserialize<List<LocalUser>>(File.ReadAllText(path)) ?? [] : [];
 public bool Register(RegisterInput input) {
  lock(gate) {
   var users = Read(); var email = input.Email.Trim().ToLowerInvariant();
   if(users.Any(u => u.Email == email)) return false;
   var user = new LocalUser(Guid.NewGuid().ToString(), input.FirstName.Trim(), input.LastName.Trim(), email, "");
   users.Add(user with { PasswordHash = hasher.HashPassword(user, input.Password) });
   Directory.CreateDirectory(Path.GetDirectoryName(path)!);
   var temporary = path + ".tmp";
   File.WriteAllText(temporary, JsonSerializer.Serialize(users)); File.Move(temporary, path, true);
   return true;
  }
 }
 public LocalUser? Authenticate(string email, string password) {
  lock(gate) {
   var user = Read().FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant());
   return user != null && hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed ? user : null;
  }
 }
}
