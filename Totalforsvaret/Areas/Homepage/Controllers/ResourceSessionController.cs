using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Controllers;

[AllowAnonymous]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class ResourceSessionController(
    UserManager<Bruker> users,
    SignInManager<Bruker> signIn,
    IAntiforgery antiforgery) : Controller
{
    [HttpGet]
    public IActionResult Token()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromBody] LoginInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var bruker = await users.FindByEmailAsync(input.Email.Trim());

        if (bruker is not Privataktor)
        {
            return Unauthorized(new
            {
                message = "Innlogging mislyktes. Bruk en privat konto og kontroller opplysningene."
            });
        }

        var result = await signIn.PasswordSignInAsync(
            bruker,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        [FromBody] RegisterInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = input.Email.Trim();

        var bruker = new Privataktor
        {
            UserName = email,
            Email = email,
            Navn = $"{input.FirstName.Trim()} {input.LastName.Trim()}"
        };

        var result = await users.CreateAsync(
            bruker,
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

        await signIn.SignInAsync(
            bruker,
            isPersistent: false);

        return Ok(new { success = true });
    }
}