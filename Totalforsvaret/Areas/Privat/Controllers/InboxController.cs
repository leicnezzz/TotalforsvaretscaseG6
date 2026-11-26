using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;
using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Services;

namespace Totalforsvaret.Areas.Privat.Controllers;

/// <summary>
/// Shows assignment threads for resources owned by the signed-in private actor.
/// </summary>
[Area("Privat"), Authorize(Policy = "KunPrivat")]
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
            InboxAudience.Private,
            selectedId);

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SendMessage(int assignmentId, string? body) =>
        Run(assignmentId, userId =>
            _inbox.SendMessageAsync(
                userId,
                InboxAudience.Private,
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