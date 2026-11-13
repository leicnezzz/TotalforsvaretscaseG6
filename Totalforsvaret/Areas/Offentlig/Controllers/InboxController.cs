using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class InboxController(
    AppDbContext db,
    UserManager<Bruker> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? selectedId)
    {
        var user = await users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var assignments = await db.ResourceAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.Resource.BrukerId == user.Id)
            .Include(assignment => assignment.Resource)
                .ThenInclude(resource => resource.Category)
            .Include(assignment => assignment.Need)
            .OrderByDescending(assignment => assignment.RequestedAtUtc)
            .ToListAsync();
        
        var messages = selectedId.HasValue
            ? await db.ResourceMessages
                .AsNoTracking()
                .Where(message =>
                    message.AssignmentId == selectedId.Value)
                .Include(message => message.Sender)
                .OrderBy(message => message.SentAtUtc)
                .ToListAsync()
            : new List<ResourceMessage>();

        var items = assignments.Select(assignment =>
            new InboxItemViewModel
            {
                AssignmentId = assignment.Id,
                ResourceId = assignment.ResourceId,

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
                NeedStatus = assignment.Need.Status,
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

        var model = new InboxViewModel
        {
            Items = items,
            SelectedId = items.Any(item =>
                    item.AssignmentId == selectedId)
                ? selectedId
                : items.FirstOrDefault()?.AssignmentId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int assignmentId)
    {
        var assignment = await GetOwnedAssignment(assignmentId);

        if (assignment == null)
            return NotFound();

        if (assignment.Status != AssignmentStatus.Requested)
            return BadRequest("Forespørselen venter ikke lenger på svar.");

        var activeNeed =
            assignment.Need.Status is "Åpen" or "Under behandling";

        if (!activeNeed)
            return BadRequest("Behovet er ikke lenger aktivt.");

        var resourceAvailable =
            assignment.Resource.Available &&
            (assignment.Resource.TilgjengeligFra == null ||
             assignment.Resource.TilgjengeligFra <= DateTime.Now);

        if (!resourceAvailable)
            return BadRequest("Ressursen er ikke lenger tilgjengelig.");

        var alreadyInUse = await db.ResourceAssignments.AnyAsync(other =>
            other.Id != assignment.Id &&
            other.ResourceId == assignment.ResourceId &&
            (other.Status == AssignmentStatus.Requested ||
             other.Status == AssignmentStatus.Assigned));

        if (alreadyInUse)
            return BadRequest(
                "Ressursen er allerede under vurdering eller tildelt.");

        assignment.Status = AssignmentStatus.Assigned;
        assignment.AssignedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        TempData["InboxMessage"] = "Forespørselen er godtatt.";
        return RedirectToAction(
            nameof(Index),
            new { selectedId = assignmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(int assignmentId)
    {
        var assignment = await GetOwnedAssignment(assignmentId);

        if (assignment == null)
            return NotFound();

        if (assignment.Status != AssignmentStatus.Requested)
            return BadRequest("Forespørselen venter ikke lenger på svar.");

        assignment.Status = AssignmentStatus.Rejected;
        assignment.ClosedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        TempData["InboxMessage"] = "Forespørselen er avslått.";
        return RedirectToAction(
            nameof(Index),
            new { selectedId = assignmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int assignmentId)
    {
        var assignment = await GetOwnedAssignment(assignmentId);

        if (assignment == null)
            return NotFound();

        if (assignment.Status != AssignmentStatus.Assigned)
            return BadRequest("Ressursen er ikke tildelt.");

        assignment.Status = AssignmentStatus.Released;
        assignment.ClosedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        TempData["InboxMessage"] = "Ressursen er frigitt.";
        return RedirectToAction(
            nameof(Index),
            new { selectedId = assignmentId });
    }

    private async Task<ResourceAssignment?> GetOwnedAssignment(
        int assignmentId)
    {
        var user = await users.GetUserAsync(User);

        if (user == null)
            return null;

        return await db.ResourceAssignments
            .Include(assignment => assignment.Resource)
            .Include(assignment => assignment.Need)
            .SingleOrDefaultAsync(assignment =>
                assignment.Id == assignmentId &&
                assignment.Resource.BrukerId == user.Id);
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

        var assignmentExists = await db.ResourceAssignments
            .AnyAsync(item => item.Id == assignmentId);

        if (!assignmentExists)
            return NotFound();

        db.ResourceMessages.Add(new ResourceMessage
        {
            AssignmentId = assignmentId,
            SenderUserId = user.Id,
            Body = body,
            SentAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        TempData["InboxMessage"] = "Svaret er sendt.";

        return RedirectToAction(nameof(Index), new
        {
            selectedId = assignmentId
        });
    }
}