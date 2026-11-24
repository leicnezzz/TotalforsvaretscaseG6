using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.Inbox;

namespace Totalforsvaret.Services;

public sealed class InboxService(IResourceAssignmentRepository repository, ResourceAssignmentService assignments) : IInboxService
{
    public async Task<InboxViewModel> GetInboxAsync(
        int userId,
        InboxAudience audience,
        int? selectedId)
    {
        var visibleAssignments = audience == InboxAudience.Private
            ? await repository.ListForResourceOwnerAsync(userId)
            : await repository.ListForPublicInboxAsync();

        var items = visibleAssignments
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
                NeedType = a.Need.Type.ToNorwegianText(),
                NeedPriority = a.Need.Priority.ToNorwegianText(),
                NeedStatus = a.Need.Status.ToNorwegianText(),
                NeedDescription = a.Need.Description,
                NeedContactPerson = a.Need.Kontaktperson,
                NeedContactPoint = a.Need.Kontaktpunkt,
                NeedPhoneNumber = a.Need.Telefonnummer,

                Status = a.Status,
                RequestedAtUtc = a.RequestedAtUtc,
                AssignedAtUtc = a.AssignedAtUtc,
                ClosedAtUtc = a.ClosedAtUtc
            })
            .ToList();

        int? actualSelectedId = items.Any(i => i.AssignmentId == selectedId)
            ? selectedId
            : items.FirstOrDefault()?.AssignmentId;

        var messages = actualSelectedId.HasValue
            ? await repository.ListMessagesAsync(actualSelectedId.Value, userId)
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

        var assignmentExists = audience == InboxAudience.Private
            ? await repository.ExistsForOwnerAsync(assignmentId, userId)
            : await repository.ExistsAsync(assignmentId);

        if (!assignmentExists)
        {
            return InboxResult.Missing();
        }

        await repository.AddMessageAsync(new ResourceMessage
        {
            AssignmentId = assignmentId,
            SenderUserId = userId,
            Body = body,
            SentAtUtc = DateTime.UtcNow
        });

        var successMessage = audience == InboxAudience.Public
            ? "Svaret er sendt."
            : "Meldingen er sendt.";

        return InboxResult.Ok(successMessage);
    }

    public async Task<InboxResult> AcceptAsync(int userId, int assignmentId)
    {
        var assignment = await repository.FindOwnedAsync(assignmentId, userId);
        var result = await assignments.AcceptAsync(assignment);

        return result switch
        {
            AssignmentActionResult.Succeeded => InboxResult.Ok("Forespørselen er godtatt."),
            AssignmentActionResult.NotFound => InboxResult.Missing(),
            AssignmentActionResult.NotRequested => InboxResult.Fail("Forespørselen venter ikke lenger på svar."),
            AssignmentActionResult.InactiveNeed => InboxResult.Fail("Behovet er ikke lenger aktivt."),
            AssignmentActionResult.UnavailableResource => InboxResult.Fail("Ressursen er ikke lenger tilgjengelig."),
            AssignmentActionResult.BusyResource => InboxResult.Fail("Ressursen er allerede under vurdering eller tildelt."),
            _ => throw new InvalidOperationException($"Unexpected assignment result: {result}.")
        };
    }

    public async Task<InboxResult> DeclineAsync(int userId, int assignmentId)
    {
        var assignment = await repository.FindOwnedAsync(assignmentId, userId);
        var result = await assignments.DeclineAsync(assignment);

        return result switch
        {
            AssignmentActionResult.Succeeded => InboxResult.Ok("Forespørselen er avslått."),
            AssignmentActionResult.NotFound => InboxResult.Missing(),
            AssignmentActionResult.NotRequested => InboxResult.Fail("Forespørselen venter ikke lenger på svar."),
            _ => throw new InvalidOperationException($"Unexpected assignment result: {result}.")
        };
    }

    public async Task<InboxResult> ReleaseAsync(int userId, int assignmentId)
    {
        var assignment = await repository.FindOwnedAsync(assignmentId, userId);
        var result = await assignments.ReleaseAsync(assignment);

        return result switch
        {
            AssignmentActionResult.Succeeded => InboxResult.Ok("Ressursen er frigitt."),
            AssignmentActionResult.NotFound => InboxResult.Missing(),
            AssignmentActionResult.NotAssigned => InboxResult.Fail("Ressursen er ikke tildelt."),
            _ => throw new InvalidOperationException($"Unexpected assignment result: {result}.")
        };
    }
}