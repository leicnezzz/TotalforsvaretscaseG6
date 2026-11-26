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
        var user = await users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        return View(new PrivateHomeViewModel
        {
			Navn = user.Navn,
			HasPhoto = await photos.ExistsAsync(user.Id)
		});
    }

    [HttpGet]
    public IActionResult ResourcePoints()
    {
        if (!int.TryParse(users.GetUserId(User), out var userId))
            return Challenge();

        var points = resources.GetResourcePointsForUser(userId);

        return Json(points);
    }
}