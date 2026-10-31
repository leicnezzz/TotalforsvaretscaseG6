using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Handlers;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class DashboardController(
    UserManager<Bruker> users,
    ResourceHandler resources) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bruker = await users.GetUserAsync(User);

        if (bruker == null)
            return Challenge();

        return View(new PrivateHomeViewModel
        {
            Navn = bruker.Navn
        });
    }

    [HttpGet]
    public IActionResult ResourcePoints()
    {
        if (!int.TryParse(users.GetUserId(User), out var brukerId))
            return Challenge();

        var points = resources.GetResourcePointsForUser(brukerId);

        return Json(points);
    }
}