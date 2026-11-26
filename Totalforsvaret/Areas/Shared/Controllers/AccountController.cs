using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;
using Totalforsvaret.Services;

namespace Totalforsvaret.Controllers;

/// <summary>
/// Handles registration and role-specific sign-in from the web forms.
/// </summary>
public class AccountController : Controller {
    private readonly UserManager<Bruker> _users;
    private readonly SignInManager<Bruker> _signIn;

    public AccountController(UserManager<Bruker> users, SignInManager<Bruker> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    /// <summary>
    /// Creates a private account and redirects the user to sign in.
    /// </summary>
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
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

        var resultat = await _users.CreateAsync(
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

    /// <summary>
    /// Signs in through the selected actor type and enables lockout on failed attempts.
    /// </summary>
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid)
        {
            return ShowLoginError(input);
        }

        var user = await _users.FindByEmailAsync(
            input.Email.Trim());

        if (user == null)
        {
            return ShowLoginError(input);
        }

        var resultat = await _signIn.PasswordSignInAsync(
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

        return RedirectToAction("Index", "OffentligAktor", new { area = "" });
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Shows the password help page while self-service password reset is unavailable.
    /// </summary>
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

    /// <summary>
    /// Signs in an existing public actor and enables lockout on failed attempts.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/Account/Login/Offentlig"), ValidateAntiForgeryToken]
    [ActionName("LoginOffentlig")]
    public async Task<IActionResult> PublicLogin(LoginInput input)
    {
        ViewData["OffentligLogin"] = true;

        if (!ModelState.IsValid)
            return ShowLoginError(input);

        var user = await _users.FindByEmailAsync(input.Email.Trim());

        if (user is not PublicActor)
            return ShowLoginError(input);

        var resultat = await _signIn.PasswordSignInAsync(
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