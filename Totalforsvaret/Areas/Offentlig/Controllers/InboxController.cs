using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;
using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Services;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

/// <summary>
/// Shows all assignment threads to public actors and reserves inbox status changes for the
/// resource owner.
/// </summary>
[Area("Offentlig"), Authorize(Policy = "KunOffentlig")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class InboxController : Controller {
    private readonly IInboxService _inbox;
    private readonly UserManager<Bruker> _users;

    public InboxController(IInboxService inbox, UserManager<Bruker> users)
    {
        _inbox = inbox;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? selectedId)
    {
        var user = await _users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var model = await _inbox.GetInboxAsync(
            user.Id,
            InboxAudience.Public,
            selectedId);

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reply(int assignmentId, string? body) =>
        Run(assignmentId, userId =>
            _inbox.SendMessageAsync(
                userId,
                InboxAudience.Public,
                assignmentId,
                body));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Accept(int assignmentId) =>
        Run(assignmentId, userId =>
            _inbox.AcceptAsync(userId, assignmentId));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Decline(int assignmentId) =>
        Run(assignmentId, userId =>
            _inbox.DeclineAsync(userId, assignmentId));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Release(int assignmentId) =>
        Run(assignmentId, userId =>
            _inbox.ReleaseAsync(userId, assignmentId));

    private async Task<IActionResult> Run(
        int assignmentId,
        Func<int, Task<InboxResult>> operation)
    {
        var user = await _users.GetUserAsync(User);

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