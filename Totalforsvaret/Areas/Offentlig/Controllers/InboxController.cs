using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class InboxController(
    IResourceAssignmentRepository assignmentRepository,
    UserManager<Bruker> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? selectedId)
    {
        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        // Alle offentlige aktører kan se alle matcher.
        var assignments = await assignmentRepository.ListForPublicInboxAsync();

        var items = assignments
            .Select(assignment => new InboxItemViewModel
            {
                AssignmentId = assignment.Id,
                ResourceId = assignment.ResourceId,

                // Bare ressursens eier får endre matchstatus.
                CanManageAssignment =
                    assignment.Resource.BrukerId == user.Id,

                ResourceName = assignment.Resource.Object,
                CategoryName = assignment.Resource.Category.Name,
                ResourceAvailable = assignment.Resource.Available,
                ResourceAddress = assignment.Resource.Address,
                ResourceCondition = assignment.Resource.Condition,
                ResourceInformation =
                    assignment.Resource.AdditionalInformation,

                NeedName = assignment.Need.Name,
                NeedType = assignment.Need.Type,
                NeedPriority = assignment.Need.Priority,
                NeedStatus = assignment.Need.Status.ToNorwegianText(),
                NeedDescription = assignment.Need.Description,
                NeedContactPerson = assignment.Need.Kontaktperson,
                NeedContactPoint = assignment.Need.Kontaktpunkt,
                NeedPhoneNumber = assignment.Need.Telefonnummer,
                NeedDeadline = assignment.Need.Frist,

                Status = assignment.Status,
                RequestedAtUtc = assignment.RequestedAtUtc,
                AssignedAtUtc = assignment.AssignedAtUtc,
                ClosedAtUtc = assignment.ClosedAtUtc
            })
            .ToList();

        // Bruk valgt match bare hvis den finnes.
        var valgtId = items.Any(item => item.AssignmentId == selectedId)
            ? selectedId
            : items.FirstOrDefault()?.AssignmentId;

        var messages = valgtId.HasValue
            ? await assignmentRepository.ListMessagesAsync(valgtId.Value, user.Id)
            : new List<InboxMessageViewModel>();

        var model = new InboxViewModel
        {
            Items = items,
            SelectedId = valgtId,
            Messages = messages
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(
        int assignmentId,
        string body)
    {
        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        body = body?.Trim() ?? "";

        if (body.Length == 0 || body.Length > 2000)
        {
            TempData["InboxMessage"] =
                "Meldingen må inneholde mellom 1 og 2000 tegn.";

            return RedirectToAction(nameof(Index), new
            {
                selectedId = assignmentId
            });
        }

        // Kun match-ID kontrolleres her. Alle KunOffentlig-brukere
        // har lov til å svare, slik du ønsker.
        if (!await assignmentRepository.ExistsAsync(assignmentId))
            return NotFound();

        await assignmentRepository.AddMessageAsync(new ResourceMessage
        {
            AssignmentId = assignmentId,
            SenderUserId = user.Id,
            Body = body,
            SentAtUtc = DateTime.UtcNow
        });

        TempData["InboxMessage"] = "Svaret er sendt.";

        return RedirectToAction(nameof(Index), new
        {
            selectedId = assignmentId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int assignmentId)
    {
        var assignment = await GetOwnedAssignment(assignmentId);

        if (assignment is null)
            return NotFound();

        if (assignment.Status != AssignmentStatus.Requested)
            return VisHandlingsfeil(assignmentId, "Forespørselen venter ikke lenger på svar.");

        var activeNeed =
            assignment.Need.Status is NeedStatus.Open or NeedStatus.InProgress;

        if (!activeNeed)
            return VisHandlingsfeil(assignmentId, "Behovet er ikke lenger aktivt.");

        var resourceAvailable =
            assignment.Resource.Available &&
            (assignment.Resource.TilgjengeligFra == null ||
             assignment.Resource.TilgjengeligFra <= DateTime.Now);

        if (!resourceAvailable)
            return VisHandlingsfeil(assignmentId, "Ressursen er ikke lenger tilgjengelig.");

        var alreadyInUse = await assignmentRepository.HasOtherActiveAssignmentAsync(
            assignment.Id, assignment.ResourceId);

        if (alreadyInUse)
        {
            return VisHandlingsfeil(assignmentId, "Ressursen er allerede under vurdering eller tildelt.");
        }

        assignment.MarkAssigned();

        await assignmentRepository.SaveChangesAsync();

        TempData["InboxMessage"] = "Forespørselen er godtatt.";

        return RedirectToAction(nameof(Index), new
        {
            selectedId = assignmentId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(int assignmentId)
    {
        var assignment = await GetOwnedAssignment(assignmentId);

        if (assignment is null)
            return NotFound();

        if (assignment.Status != AssignmentStatus.Requested)
            return VisHandlingsfeil(assignmentId, "Forespørselen venter ikke lenger på svar.");

        assignment.MarkRejected();

        await assignmentRepository.SaveChangesAsync();

        TempData["InboxMessage"] = "Forespørselen er avslått.";

        return RedirectToAction(nameof(Index), new
        {
            selectedId = assignmentId
        });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int assignmentId)
    {
        var assignment = await GetOwnedAssignment(assignmentId);

        if (assignment is null)
            return NotFound();

        if (assignment.Status != AssignmentStatus.Assigned)
            return VisHandlingsfeil(assignmentId, "Ressursen er ikke tildelt.");

        assignment.MarkReleased();

        await assignmentRepository.SaveChangesAsync();

        TempData["InboxMessage"] = "Ressursen er frigitt.";

        return RedirectToAction(nameof(Index), new
        {
            selectedId = assignmentId
        });
    }

    private IActionResult VisHandlingsfeil(int assignmentId, string melding)
    {
        TempData["InboxMessage"] = melding;
        return RedirectToAction(nameof(Index), new { selectedId = assignmentId });
    }

    private async Task<ResourceAssignment?> GetOwnedAssignment(
        int assignmentId)
    {
        var user = await users.GetUserAsync(User);

        if (user is null)
            return null;

        // Denne kontrollen er med vilje fortsatt eierbegrenset.
        // At alle kan svare på meldinger betyr ikke at alle kan
        // godta eller avslå en annens ressursmatch.
        return await assignmentRepository.FindOwnedAsync(assignmentId, user.Id);
    }
}