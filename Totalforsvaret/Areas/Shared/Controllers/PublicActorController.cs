using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Services;
namespace Totalforsvaret.Controllers;

[Authorize(Policy = "KunOffentlig")]

/// <summary>
/// Provides the public actor overview and resource coordination actions.
/// </summary>
[MvcControllerName("OffentligAktor")]
public class PublicActorController : Controller
{
    private readonly IResourceRepository _resources;
    private readonly IResourceAssignmentRepository _assignments;
    private readonly IPublicOverviewRepository _overview;
    private readonly ResourceMatchingService _matching;
    private readonly ResourceAssignmentService _assignmentService;

    public PublicActorController(
        IResourceRepository resources,
        IResourceAssignmentRepository assignments,
        IPublicOverviewRepository overview,
        ResourceMatchingService matching,
        ResourceAssignmentService assignmentService)
    {
        _resources = resources;
        _assignments = assignments;
        _overview = overview;
        _matching = matching;
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Builds match suggestions and the operational overview. Active assignments take precedence
    /// over closed reviews when displaying match status.
    /// </summary>
    public IActionResult Index()
    {
        var availableResources = _resources.GetResources()
            .Where(resource => resource.Available)
            .ToList();
        var activeAssignments = _overview.ListActiveAssignments();
        var closedReviews = _overview.ListClosedReviewsForActiveNeeds();

        var matchStatuses = activeAssignments
            .OrderByDescending(assignment => assignment.Id)
            .Concat(closedReviews.OrderByDescending(assignment => assignment.Id))
            .GroupBy(assignment => (assignment.NeedId, assignment.ResourceId))
            .ToDictionary(group => group.Key, group => group.First().Status);

        TryGetUserId(out var currentUserId);
        return View(new PublicActorOverviewViewModel
        {
            CurrentUserId = currentUserId,
            ActiveNeedCount = _overview.CountActiveNeeds(),
            AvailableResources = availableResources,
            MapNeeds = _overview.ListActiveNeedsOnMap(),
            MapAnnotations = _overview.ListAnnotations(),
            Crises = _overview.ListCrises(),
            Needs = _overview.ListNeeds(),
            ActiveAssignments = activeAssignments,
            BusyResourceIds = activeAssignments.Select(assignment => assignment.ResourceId).ToHashSet(),
            MatchStatuses = matchStatuses,
            MatchSuggestions = _matching.FindSuggestions(_overview.ListActiveNeeds(), availableResources, matchStatuses)
        });
    }

    /// <summary>
    /// Lists resources owned by the signed-in public actor.
    /// </summary>
    [HttpGet]
    [ActionName("MineRessurser")]
    public async Task<IActionResult> MyResources()
    {
        if (!TryGetUserId(out var userId)) return Challenge();

        var resources = await _resources.ListOwnedAsync(userId);

        return View(resources);
    }

    /// <summary>
    /// Deletes an owned resource if it has no assignment history.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    [ActionName("SlettRessurs")]
    public async Task<IActionResult> DeleteResource(int resourceId)
    {
        if (!TryGetUserId(out var userId)) return Challenge();

        var ressurs = await _resources.FindOwnedAsync(resourceId, userId);

        if (ressurs == null)
            return NotFound("Ressursen finnes ikke.");

        if (!await _resources.TryDeleteAsync(ressurs))
        {
            TempData["ErrorMessage"] = "Ressursen har vært brukt i en tildeling og kan ikke slettes.";
            return LocalRedirect(Url.Action("MineRessurser") + Request.CrisisQuery());
        }

        TempData["SuccessMessage"] = "Ressursen er slettet.";

        return LocalRedirect(Url.Action("MineRessurser") + Request.CrisisQuery());
    }

    /// <summary>
    /// Requests resources for one shared need or pairs each selected resource with a selected
    /// need.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    [ActionName("VurderRessurs")]
    public async Task<IActionResult> RequestAssignment(int[] needId, int[] resourceId, bool fraMatch = false)
    {
        if (resourceId.Length == 0 || (needId.Length != 1 && needId.Length != resourceId.Length) ||
            resourceId.Distinct().Count() != resourceId.Length ||
            needId.Distinct().Count() != needId.Length)
            return ShowMatchError("Velg ett behov eller ett behov per ressurs.", fraMatch);

        var result = await _assignments.RequestAsync(needId, resourceId);
        var errorMessage = result switch
        {
            RequestAssignmentResult.InactiveNeed => "Et valgt behov finnes ikke eller er avsluttet.",
            RequestAssignmentResult.UnavailableResource => "En valgt ressurs er ikke tilgjengelig.",
            RequestAssignmentResult.BusyResource => "En valgt ressurs er allerede under vurdering eller tildelt.",
            _ => null
        };
        if (errorMessage is not null)
            return ShowMatchError(errorMessage, fraMatch);

        TempData["SuccessMessage"] = resourceId.Length == 1
            ? "Ressursen er sendt til vurdering."
            : "Ressursene er sendt til vurdering.";

        return LocalRedirect(this.OverviewUrl());
    }

    private IActionResult ShowMatchError(string melding, bool fraMatch)
    {
        TempData["MatchError"] = melding;
        return LocalRedirect(fraMatch
            ? Url.RouteUrl("offentlig-match") + Request.CrisisQuery()
            : this.OverviewUrl());
    }

    /// <summary>
    /// Lets the resource owner release an assigned resource from the overview and retains the closed
    /// assignment as history.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    [ActionName("FrigiRessurs")]
    public async Task<IActionResult> ReleaseResource(int assignmentId)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();

        var assignment = await _assignments.FindOwnedAsync(assignmentId, userId);
        var result = await _assignmentService.ReleaseAsync(assignment);
        if (result == AssignmentActionResult.NotFound)
            return NotFound("Tildelingen finnes ikke.");

        if (result == AssignmentActionResult.NotAssigned)
            return BadRequest("Ressursen er ikke tildelt.");

        TempData["SuccessMessage"] = "Ressursen er frigitt.";

        return LocalRedirect(this.OverviewUrl());
    }

    private bool TryGetUserId(out int userId)
    {
        var tekst = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(tekst, out userId);
    }
}
