using Totalforsvaret.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat"), Authorize(Policy = "KunPrivat")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
/// <summary>
/// Shows the private actor dashboard and the signed-in user's resource locations.
/// </summary>
public class DashboardController : Controller {
    private readonly UserManager<Bruker> _users;
    private readonly IResourceRepository _resources;
    private readonly IProfilePhotoRepository _photos;

    public DashboardController(
        UserManager<Bruker> users,
        IResourceRepository resources,
        IProfilePhotoRepository photos)
    {
        _users = users;
        _resources = resources;
        _photos = photos;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        return View(new PrivateHomeViewModel
        {
			Navn = user.Navn,
			HasPhoto = await _photos.ExistsAsync(user.Id)
		});
    }

    /// <summary>
    /// Returns map points for resources owned by the signed-in actor.
    /// </summary>
    [HttpGet]
    public IActionResult ResourcePoints()
    {
        if (!int.TryParse(_users.GetUserId(User), out var userId))
            return Challenge();

        var points = _resources.GetResourcePointsForUser(userId);

        return Json(points);
    }
}