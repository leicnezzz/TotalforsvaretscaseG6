using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Totalforsvaret.Models;
using Totalforsvaret.Services;
namespace Totalforsvaret.Controllers;
public class AccountController(LocalUsers users) : Controller {
 [HttpGet] public IActionResult Login() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();
 [HttpGet] public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();
 [HttpGet] public IActionResult ForgotPassword() => View();
 [HttpPost, ValidateAntiForgeryToken]
 public IActionResult Register(RegisterInput input) {
  if(!ModelState.IsValid) { ViewData["FormError"] = "Kontroller feltene. Passordet må ha minst 12 tegn, bokstaver og tall, og passordene må være like."; return View("~/Views/Account/Register.cshtml"); }
  if(!users.Register(input)) { ViewData["FormError"] = "Denne e-postadressen er allerede registrert. Logg inn i stedet."; return View("~/Views/Account/Register.cshtml"); }
  TempData["AccountMessage"] = "Brukeren er opprettet. Logg inn med e-post og passord.";
  return RedirectToAction("Login");
 }
 [HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Login(LoginInput input) {
  var user = ModelState.IsValid && input.Actor == "civil" ? users.Authenticate(input.Email, input.Password) : null;
  if(user == null) { ViewData["FormError"] = "Feil e-post, passord eller aktørtype."; return View("~/Views/Account/Login.cshtml"); }
  var identity = new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user.Id),new Claim(ClaimTypes.Name,user.FirstName),new Claim(ClaimTypes.Email,user.Email)},CookieAuthenticationDefaults.AuthenticationScheme);
  await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity),new AuthenticationProperties{IsPersistent=input.RememberMe});
  return RedirectToAction("Index", "Home");
 }
 [Authorize, HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction("Login"); }
}
