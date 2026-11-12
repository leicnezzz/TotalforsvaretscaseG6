using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;
using Totalforsvaret.DataAccess; // Change this to the namespace of your DbContext

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
public class EgenberedskapController(
    AppDbContext db,
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

        ViewData["CompletedChecklistItems"] = await db.EgenberedskapChecklistStatuses
            .Where(status => status.BrukerId == user.Id && status.IsComplete)
            .Select(status => status.ItemId)
            .ToHashSetAsync();

        ViewData["ChecklistItemCount"] = ValidItemIds.Count;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetChecklistStatus(string itemId, bool isComplete)
    {
        var user = await users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        if (!ValidItemIds.Contains(itemId))
            return BadRequest();

        var status = await db.EgenberedskapChecklistStatuses
            .FindAsync(user.Id, itemId);

        if (status == null)
        {
            status = new EgenberedskapChecklistStatus
            {
                BrukerId = user.Id,
                ItemId = itemId
            };

            db.EgenberedskapChecklistStatuses.Add(status);
        }

        status.IsComplete = isComplete;
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), controllerName: null, routeValues: null, fragment: "checklist");
    }
}