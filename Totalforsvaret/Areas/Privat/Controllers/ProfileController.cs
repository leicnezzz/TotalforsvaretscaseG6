using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Privat.Controllers;
//Arver fra Usermanagement for at kun privat brukere har tilgang
[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
public class ProfileController(UserManager<Bruker> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var nameParts = user.Navn
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var fornavn = nameParts.Length > 1
            ? string.Join(' ', nameParts[..^1])
            : nameParts.FirstOrDefault() ?? "";

        var etternavn = nameParts.Length > 1
            ? nameParts[^1]
            : "";

        var model = new ProfileViewModel
        {
            Fornavn = fornavn,
            Etternavn = etternavn,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        if (!ModelState.IsValid)
        {
            model.Email = user.Email;
            return View("Index", model);
        }

        user.Navn = $"{model.Fornavn.Trim()} {model.Etternavn.Trim()}".Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        var result = await users.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            model.Email = user.Email;
            return View("Index", model);
        }

        TempData["ProfileSaved"] = "Profilen din er oppdatert.";
        return RedirectToAction(nameof(Index));
    }
}