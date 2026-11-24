using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;
using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Services;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class InboxController(
    IInboxService inbox,
    UserManager<Bruker> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? selectedId)
    {
        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var model = await inbox.GetInboxAsync(
            user.Id,
            InboxAudience.Public,
            selectedId);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reply(int assignmentId, string? body) =>
        Run(assignmentId, userId =>
            inbox.SendMessageAsync(
                userId,
                InboxAudience.Public,
                assignmentId,
                body));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Accept(int assignmentId) =>
        Run(assignmentId, userId =>
            inbox.AcceptAsync(userId, assignmentId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Decline(int assignmentId) =>
        Run(assignmentId, userId =>
            inbox.DeclineAsync(userId, assignmentId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Release(int assignmentId) =>
        Run(assignmentId, userId =>
            inbox.ReleaseAsync(userId, assignmentId));

    private async Task<IActionResult> Run(
        int assignmentId,
        Func<int, Task<InboxResult>> operation)
    {
        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var result = await operation(user.Id);

        if (result.Outcome == InboxOutcome.NotFound)
            return NotFound();

        TempData["InboxMessage"] = result.Message;

        return RedirectToAction(
            nameof(Index),
            new { selectedId = assignmentId });
    }
}