using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Privat.Controllers;

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

        return View("Index", new ProfileViewModel
        {
            Fornavn = user.Navn,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        });
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
            return View(model);
        }

        user.Navn = model.Fornavn.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        var result = await users.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            model.Email = user.Email;
            return View(model);
        }

        TempData["ProfileSaved"] = "Profilen din er oppdatert.";
        return RedirectToAction(nameof(Index));
    }
}