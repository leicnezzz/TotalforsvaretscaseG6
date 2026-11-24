using Totalforsvaret.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class DashboardController(
    UserManager<Bruker> users,
		IResourceRepository resources,
	IProfilePhotoRepository photos) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bruker = await users.GetUserAsync(User);

        if (bruker == null)
            return Challenge();

        return View(new PrivateHomeViewModel
        {
			Navn = bruker.Navn,
			HasPhoto = await photos.ExistsAsync(bruker.Id)
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