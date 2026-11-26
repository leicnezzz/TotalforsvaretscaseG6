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
/// <summary>
/// Shows the private actor dashboard and the signed-in user's resource locations.
/// </summary>
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

    /// <summary>
    /// Returns map points for resources owned by the signed-in actor.
    /// </summary>
    [HttpGet]
    public IActionResult ResourcePoints()
    {
        if (!int.TryParse(users.GetUserId(User), out var userId))
            return Challenge();

        var points = resources.GetResourcePointsForUser(userId);

        return Json(points);
    }
}