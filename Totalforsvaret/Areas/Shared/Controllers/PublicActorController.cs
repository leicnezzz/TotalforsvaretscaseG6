using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Services;
namespace Totalforsvaret.Controllers;

[Authorize(Policy = "KunOffentlig")]

/// <summary> 
/// 
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

    public IActionResult Index()
    {
        // Henter tilgjengelige ressurser
        var resources = _resources.GetResources();

        var availableResources = resources
            .Where(r => r.Available)
            .ToList();

        // Henter antall aktive behov
        ViewBag.AktiveBehov = _overview.CountActiveNeeds();

        // Henter antall løste behov.
        ViewBag.LosteBehov = _overview.CountResolvedNeeds();

        // Henter aktive behov med gyldige kartkoordinater
        ViewBag.BehovPaKart = _overview.ListActiveNeedsOnMap();

        // Henter de fem nyeste aktive behovene
        ViewBag.AktiveBehovListe = _overview.ListLatestActiveNeeds();

        // Alle hendelser kan åpnes, også inaktive og hendelser uten eget kartpunkt.
        ViewBag.Kartmarkeringer = _overview.ListAnnotations();
        var crises = _overview.ListCrises();
        ViewBag.KriserPaKart = crises.Select(c => new
            {
                id = c.Id,
                navn = c.Name,
                type = c.Type.ToNorwegianText(),
                alvorlighetsgrad = c.Severity.ToNorwegianText(),
                status = c.Status.ToNorwegianText(),
                beskrivelse = c.Description,
                latitude = c.Latitude,
                longitude = c.Longitude
            })
            .ToList();

        // Alle aktive og løste behov til kortenes lister.
        ViewBag.BehovTilLister = _overview.ListNeeds().Select(n => new
            {
                id = n.Id,
                navn = n.Name,
                type = n.Type.ToNorwegianText(),
                prioritet = n.Priority.ToNorwegianText(),
                status = n.Status.ToNorwegianText(),
                kriseId = n.CrisisId,
                krise = n.CrisisName,
                latitude = n.Latitude,
                longitude = n.Longitude
            })
            .ToList();

        // Teller kriser med status Aktiv.
        ViewBag.AktiveKriser = _overview.CountActiveCrises();
        var activeAssignments = _overview.ListActiveAssignments();

        ViewBag.AktiveTildelinger = activeAssignments;

		var closedReviews = _overview.ListClosedReviewsForActiveNeeds();

		var matchStatuses = activeAssignments
			.OrderByDescending(a => a.Id)
			.Concat(closedReviews.OrderByDescending(a => a.Id))
			.GroupBy(a => (a.NeedId, a.ResourceId))
			.ToDictionary(group => group.Key, group => group.First().Status);

		ViewBag.TreffStatuser = matchStatuses;

        var suggestions = _matching.FindSuggestions(_overview.ListActiveNeeds(), availableResources, matchStatuses);
        ViewBag.MatchForslag = suggestions;
        ViewBag.ForeslatteTreff = suggestions.Count;
        ViewBag.OpptatteRessursIder = activeAssignments.Select(a => a.ResourceId).ToHashSet();

        ViewBag.KriserTilLister = crises.Select(c => new
            {
                id = c.Id,
                navn = c.Name,
                type = c.Type.ToNorwegianText(),
                omrade = c.Area,
                alvorlighetsgrad = c.Severity.ToNorwegianText(),
                starttidspunkt = c.StartTime,
                status = c.Status.ToNorwegianText(),
                beskrivelse = c.Description,
                latitude = c.Latitude,
                longitude = c.Longitude
            })
            .ToList();

        // Brukes til å markere egne ressurser på kartet.
        TryGetUserId(out var currentUserId);
        ViewBag.MinBrukerId = currentUserId;

        return View(availableResources);
    }
    [HttpGet]
    [ActionName("MineRessurser")]
    public async Task<IActionResult> MyResources()
    {
        if (!TryGetUserId(out var userId)) return Challenge();

        var resources = await _resources.ListOwnedWithCategoryAsync(userId);

        return View(resources);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("SlettRessurs")]
    public async Task<IActionResult> DeleteResource(int resourceId)
    {
        if (!TryGetUserId(out var userId)) return Challenge();

        // Bare egne ressurser kan slettes.
        var ressurs = await _resources.FindOwnedAsync(resourceId, userId);

        if (ressurs == null)
            return NotFound("Ressursen finnes ikke.");

        // Ressurser som har vært i en tildeling beholdes på grunn av historikken.
        if (!await _resources.TryDeleteAsync(ressurs))
        {
            TempData["ErrorMessage"] = "Ressursen har vært brukt i en tildeling og kan ikke slettes.";
            return LocalRedirect(Url.Action("MineRessurser") + Request.CrisisQuery());
        }

        TempData["SuccessMessage"] = "Ressursen er slettet.";

        return LocalRedirect(Url.Action("MineRessurser") + Request.CrisisQuery());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("FrigiRessurs")]
    public async Task<IActionResult> ReleaseResource(int assignmentId)
    {
        var assignment = await _assignments.FindAsync(assignmentId);
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
