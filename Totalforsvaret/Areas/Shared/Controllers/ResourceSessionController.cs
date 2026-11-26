using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;
using Totalforsvaret.Services;

namespace Totalforsvaret.Home.Controllers;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
/// <summary>
/// Provides session endpoints for the resource registration wizard.
/// </summary>
public class ResourceSessionController : Controller {
    private readonly UserManager<Bruker> _users;
    private readonly SignInManager<Bruker> _signIn;
    private readonly IAntiforgery _antiforgery;

    public ResourceSessionController(
        UserManager<Bruker> users,
        SignInManager<Bruker> signIn,
        IAntiforgery antiforgery)
    {
        _users = users;
        _signIn = signIn;
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Issues the anti-forgery token used by the resource wizard.
    /// </summary>
    [HttpGet]
    public IActionResult Token()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new { token = tokens.RequestToken });
    }

    /// <summary>
    /// Signs in a private actor through the wizard without creating a persistent session.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromBody] LoginInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _users.FindByEmailAsync(input.Email.Trim());

        if (user is not PrivateActor)
        {
            return Unauthorized(new
            {
                message = "Innlogging mislyktes. Bruk en privat konto og kontroller opplysningene."
            });
        }

        var result = await _signIn.PasswordSignInAsync(
            user,
            input.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Innlogging mislyktes. Kontroller opplysningene. Kontoen kan være midlertidig låst."
            });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Creates and signs in a private actor directly from the resource wizard.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        [FromBody] RegisterInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = PrivateAccountRegistration.CreateUser(input);

        var result = await _users.CreateAsync(
            user,
            input.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = string.Join(
                    " ",
                    result.Errors.Select(error => error.Description))
            });
        }

        await _signIn.SignInAsync(
            user,
            isPersistent: false);

        return Ok(new { success = true });
    }
}