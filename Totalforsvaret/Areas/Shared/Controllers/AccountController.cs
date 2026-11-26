using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;
using Totalforsvaret.Services;

namespace Totalforsvaret.Controllers;

public class AccountController(
    UserManager<Bruker> users,
    SignInManager<Bruker> signIn) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!ModelState.IsValid)
        {
            ViewData["FormError"] =
                "Kontroller feltene. Passordet må ha minst " +
                "12 tegn, bokstaver og tall. Passordene må være like.";

            return View(input);
        }

        var user = PrivateAccountRegistration.CreateUser(input);

        var resultat = await users.CreateAsync(
            user,
            input.Password);

        if (!resultat.Succeeded)
        {
            ViewData["FormError"] = string.Join(
                " ",
                resultat.Errors.Select(feil => feil.Description));

            return View(input);
        }

        TempData["AccountMessage"] =
            "Brukeren er opprettet. Du kan nå logge inn.";

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid)
        {
            return ShowLoginError(input);
        }

        var user = await users.FindByEmailAsync(
            input.Email.Trim());

        // Valget i skjemaet må samsvare med lagret brukertype.
        // Brukertypen hentes fra kontoen i databasen.
        if (user == null)
        {
            return ShowLoginError(input);
        }

        var resultat = await signIn.PasswordSignInAsync(
            user,
            input.Password,
            isPersistent: input.RememberMe,
            lockoutOnFailure: true);

        if (!resultat.Succeeded)
        {
            return ShowLoginError(input);
        }

        if (user is PrivateActor)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Privat" });
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }


    private IActionResult ShowLoginError(LoginInput input)
    {
        ViewData["FormError"] =
             "Innlogging mislyktes. Kontroller e-post og passord. " +
             "Kontoen kan være midlertidig låst.";

        return View("Login", input);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden; // Venter på vi tar designet fra figma. 
        return View();
    }

[AllowAnonymous]
    [HttpGet("/Account/Login/Offentlig")]
    [ActionName("LoginOffentlig")]
    public IActionResult PublicLogin()
    {
        ViewData["OffentligLogin"] = true;
        return View("Login");
    }

    [AllowAnonymous]
    [HttpPost("/Account/Login/Offentlig")]
    [ValidateAntiForgeryToken]
    [ActionName("LoginOffentlig")]
    public async Task<IActionResult> PublicLogin(LoginInput input)
    {
        ViewData["OffentligLogin"] = true;

        if (!ModelState.IsValid)
            return ShowLoginError(input);

        var user = await users.FindByEmailAsync(input.Email.Trim());

        // Offentlig innlogging godtar bare en lagret offentlig user.
        if (user is not PublicActor)
            return ShowLoginError(input);

        var resultat = await signIn.PasswordSignInAsync(
            user,
            input.Password,
            isPersistent: input.RememberMe,
            lockoutOnFailure: true);

        if (!resultat.Succeeded)
            return ShowLoginError(input);

        return RedirectToAction(
            "Index",
            "OffentligAktor",
            new { area = "" });
    }
}