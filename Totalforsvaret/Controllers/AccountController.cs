using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

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

        var email = input.Email.Trim();

        // Vanlig registrering oppretter alltid en privat bruker.
        var bruker = new Privataktor
        {
            UserName = email,
            Email = email,
            Navn = $"{input.FirstName.Trim()} {input.LastName.Trim()}"
        };

        var resultat = await users.CreateAsync(
            bruker,
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
            return LoginFeil(input);
        }

        var bruker = await users.FindByEmailAsync(
            input.Email.Trim());

        // Valget i skjemaet må samsvare med lagret brukertype.
        var riktigType = input.Actor switch
        {
            "civil" => bruker is Privataktor,
            "public" => bruker is Offentligaktør,
            _ => false
        };

        if (bruker == null || !riktigType)
        {
            return LoginFeil(input);
        }

        var resultat = await signIn.PasswordSignInAsync(
            bruker,
            input.Password,
            isPersistent: input.RememberMe,
            lockoutOnFailure: true);

        if (!resultat.Succeeded)
        {
            return LoginFeil(input);
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


    private IActionResult LoginFeil(LoginInput input)
    {
        ViewData["FormError"] =
            "Innlogging mislyktes. Kontroller e-post, passord " +
            "og aktørtype. Kontoen kan være midlertidig låst.";

        return View("Login", input);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden; // Venter på vi tar designet fra figma. 
        return View();
    }
}


