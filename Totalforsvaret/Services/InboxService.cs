using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.Inbox;

namespace Totalforsvaret.Services;

public sealed class InboxService(AppDbContext db) : IInboxService
{
    public async Task<InboxViewModel> GetInboxAsync(
        int userId,
        InboxAudience audience,
        int? selectedId)
    {
        var query = db.ResourceAssignments.AsNoTracking();

        if (audience == InboxAudience.Private)
        {
            query = query.Where(a => a.Resource.BrukerId == userId);
        }

        var items = await query
            .OrderByDescending(a => a.RequestedAtUtc)
            .Select(a => new InboxItemViewModel
            {
                AssignmentId = a.Id,
                ResourceId = a.ResourceId,
                CanManageAssignment = a.Resource.BrukerId == userId,

                ResourceName = a.Resource.Object,
                CategoryName = a.Resource.Category.Name,
                ResourceAvailable = a.Resource.Available,
                ResourceAddress = a.Resource.Address,
                ResourceCondition = a.Resource.Condition,
                ResourceInformation = a.Resource.AdditionalInformation,

                NeedName = a.Need.Name,
                NeedType = a.Need.Type,
                NeedPriority = a.Need.Priority,
                NeedStatus = a.Need.Status,
                NeedDescription = a.Need.Description,
                NeedContactPerson = a.Need.Kontaktperson,
                NeedContactPoint = a.Need.Kontaktpunkt,
                NeedPhoneNumber = a.Need.Telefonnummer,

                Status = a.Status,
                RequestedAtUtc = a.RequestedAtUtc,
                AssignedAtUtc = a.AssignedAtUtc,
                ClosedAtUtc = a.ClosedAtUtc
            })
            .ToListAsync();

        int? actualSelectedId = items.Any(i => i.AssignmentId == selectedId)
            ? selectedId
            : items.FirstOrDefault()?.AssignmentId;

        var messages = actualSelectedId.HasValue
            ? await db.ResourceMessages
                .AsNoTracking()
                .Where(m => m.AssignmentId == actualSelectedId.Value)
                .OrderBy(m => m.SentAtUtc)
                .Select(m => new InboxMessageViewModel
                {
                    SenderName = m.Sender.Navn,
                    Body = m.Body,
                    SentAtUtc = m.SentAtUtc,
                    IsOwnMessage = m.SenderUserId == userId
                })
                .ToListAsync()
            : new List<InboxMessageViewModel>();

        return new InboxViewModel
        {
            Audience = audience,
            Items = items,
            SelectedId = actualSelectedId,
            Messages = messages
        };
    }

    public async Task<InboxResult> SendMessageAsync(
        int userId,
        InboxAudience audience,
        int assignmentId,
        string? body)
    {
        body = body?.Trim() ?? "";

        if (body.Length is 0 or > 2000)
        {
            return InboxResult.Fail(
                "Meldingen må inneholde mellom 1 og 2000 tegn.");
        }

        var assignmentQuery = db.ResourceAssignments
            .Where(a => a.Id == assignmentId);

        // Private users may message only about resources they own.
        // Public users may reply to any existing match.
        if (audience == InboxAudience.Private)
        {
            assignmentQuery = assignmentQuery
                .Where(a => a.Resource.BrukerId == userId);
        }

        var assignmentExists = await assignmentQuery.AnyAsync();

        if (!assignmentExists)
        {
            return InboxResult.Missing();
        }

        db.ResourceMessages.Add(new ResourceMessage
        {
            AssignmentId = assignmentId,
            SenderUserId = userId,
            Body = body,
            SentAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var successMessage = audience == InboxAudience.Public
            ? "Svaret er sendt."
            : "Meldingen er sendt.";

        return InboxResult.Ok(successMessage);
    }

    public async Task<InboxResult> AcceptAsync(
        int userId,
        int assignmentId)
    {
        var assignment = await FindOwnedAssignmentAsync(userId, assignmentId);

        if (assignment is null)
            return InboxResult.Missing();

        if (assignment.Status != AssignmentStatus.Requested)
        {
            return InboxResult.Fail(
                "Forespørselen venter ikke lenger på svar.");
        }

        if (assignment.Need.Status is not ("Åpen" or "Under behandling"))
        {
            return InboxResult.Fail("Behovet er ikke lenger aktivt.");
        }

        // Keep the existing local-time comparison for now.
        // Confirm the stored time convention before changing this.
        var available = assignment.Resource.Available &&
            (assignment.Resource.TilgjengeligFra == null ||
             assignment.Resource.TilgjengeligFra <= DateTime.Now);

        if (!available)
        {
            return InboxResult.Fail(
                "Ressursen er ikke lenger tilgjengelig.");
        }

        var alreadyInUse = await db.ResourceAssignments.AnyAsync(other =>
            other.Id != assignment.Id &&
            other.ResourceId == assignment.ResourceId &&
            (other.Status == AssignmentStatus.Requested ||
             other.Status == AssignmentStatus.Assigned));

        if (alreadyInUse)
        {
            return InboxResult.Fail(
                "Ressursen er allerede under vurdering eller tildelt.");
        }

        assignment.Status = AssignmentStatus.Assigned;
        assignment.AssignedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return InboxResult.Ok("Forespørselen er godtatt.");
    }

    public async Task<InboxResult> DeclineAsync(
        int userId,
        int assignmentId)
    {
        var assignment = await FindOwnedAssignmentAsync(userId, assignmentId);

        if (assignment is null)
            return InboxResult.Missing();

        if (assignment.Status != AssignmentStatus.Requested)
        {
            return InboxResult.Fail(
                "Forespørselen venter ikke lenger på svar.");
        }

        assignment.Status = AssignmentStatus.Rejected;
        assignment.ClosedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return InboxResult.Ok("Forespørselen er avslått.");
    }

    public async Task<InboxResult> ReleaseAsync(
        int userId,
        int assignmentId)
    {
        var assignment = await FindOwnedAssignmentAsync(userId, assignmentId);

        if (assignment is null)
            return InboxResult.Missing();

        if (assignment.Status != AssignmentStatus.Assigned)
        {
            return InboxResult.Fail("Ressursen er ikke tildelt.");
        }

        assignment.Status = AssignmentStatus.Released;
        assignment.ClosedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return InboxResult.Ok("Ressursen er frigitt.");
    }

    private Task<ResourceAssignment?> FindOwnedAssignmentAsync(
        int userId,
        int assignmentId)
    {
        return db.ResourceAssignments
            .Include(a => a.Resource)
            .Include(a => a.Need)
            .SingleOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.Resource.BrukerId == userId);
    }
}