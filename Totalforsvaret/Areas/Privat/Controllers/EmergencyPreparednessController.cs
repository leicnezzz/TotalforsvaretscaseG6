using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;
using Totalforsvaret.DataAccess;

namespace Totalforsvaret.Areas.Privat.Controllers;

/// <summary>
/// Displays emergency preparedness guidance and saves checklist progress for private actors.
/// </summary>
[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
[Totalforsvaret.Controllers.MvcControllerName("Egenberedskap")]
public class EmergencyPreparednessController(
    IChecklistRepository checklist,
    UserManager<Bruker> users) : Controller
{
    private static readonly HashSet<string> ValidItemIds =
    [
        "water",
        "stored-food",
        "cooking-equipment",
        "fuel",
        "pet-supplies",
        "cash",
        "warm-clothes",
        "matches",
        "firewood",
        "heater",
        "overnight",
        "flashlights",
        "radio",
        "batteries",
        "phone-list",
        "medicine",
        "iodine",
        "hygiene"
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        ViewData["CompletedChecklistItems"] = await checklist.GetCompletedAsync(user.Id);

        ViewData["ChecklistItemCount"] = ValidItemIds.Count;
        return View();
    }

    /// <summary>
    /// Updates the caller's saved progress when the item ID belongs to the supported checklist.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetChecklistStatus(string itemId, bool isComplete)
    {
        var user = await users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        if (!ValidItemIds.Contains(itemId))
            return BadRequest();

        await checklist.SetStatusAsync(user.Id, itemId, isComplete);

        return RedirectToAction(nameof(Index), controllerName: null, routeValues: null, fragment: "checklist");
    }
}