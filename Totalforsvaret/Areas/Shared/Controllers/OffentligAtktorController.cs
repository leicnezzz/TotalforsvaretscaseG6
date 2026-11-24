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
public class OffentligAktorController : Controller
{
    private readonly IResourceRepository _resources;
    private readonly IResourceAssignmentRepository _assignments;
    private readonly IPublicOverviewRepository _overview;
    private readonly ResourceMatchingService _matching;
    private readonly ResourceAssignmentService _assignmentService;

    public OffentligAktorController(
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
        var ressurser = _resources.GetResources();

        var tilgjengeligeRessurser = ressurser
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
        var kriser = _overview.ListCrises();
        ViewBag.KriserPaKart = kriser.Select(c => new
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
                type = n.Type,
                prioritet = n.Priority,
                status = n.Status.ToNorwegianText(),
                kriseId = n.CrisisId,
                krise = n.CrisisName,
                latitude = n.Latitude,
                longitude = n.Longitude
            })
            .ToList();

        // Teller kriser med status Aktiv.
        ViewBag.AktiveKriser = _overview.CountActiveCrises();
        var aktiveTildelinger = _overview.ListActiveAssignments();

        ViewBag.AktiveTildelinger = aktiveTildelinger;

		var avsluttedeVurderinger = _overview.ListClosedReviewsForActiveNeeds();

		var treffStatuser = aktiveTildelinger
			.OrderByDescending(a => a.Id)
			.Concat(avsluttedeVurderinger.OrderByDescending(a => a.Id))
			.GroupBy(a => (a.NeedId, a.ResourceId))
			.ToDictionary(gruppe => gruppe.Key, gruppe => gruppe.First().Status);

		ViewBag.TreffStatuser = treffStatuser;

        var forslag = _matching.FindSuggestions(_overview.ListActiveNeeds(), tilgjengeligeRessurser, treffStatuser);
        ViewBag.MatchForslag = forslag;
        ViewBag.ForeslatteTreff = forslag.Count;
        ViewBag.OpptatteRessursIder = aktiveTildelinger.Select(a => a.ResourceId).ToHashSet();

        ViewBag.KriserTilLister = kriser.Select(c => new
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
        TryHentBrukerId(out var minBrukerId);
        ViewBag.MinBrukerId = minBrukerId;

        return View(tilgjengeligeRessurser);
    }
    [HttpGet]
    public async Task<IActionResult> MineRessurser()
    {
        if (!TryHentBrukerId(out var brukerId)) return Challenge();

        var ressurser = await _resources.ListOwnedWithCategoryAsync(brukerId);

        return View(ressurser);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SlettRessurs(int resourceId)
    {
        if (!TryHentBrukerId(out var brukerId)) return Challenge();

        // Bare egne ressurser kan slettes.
        var ressurs = await _resources.FindOwnedAsync(resourceId, brukerId);

        if (ressurs == null)
            return NotFound("Ressursen finnes ikke.");

        // Ressurser som har vært i en tildeling beholdes på grunn av historikken.
        if (!await _resources.TryDeleteAsync(ressurs))
        {
            TempData["ErrorMessage"] = "Ressursen har vært brukt i en tildeling og kan ikke slettes.";
            return LocalRedirect(Url.Action(nameof(MineRessurser)) + Request.HendelsesQuery());
        }

        TempData["SuccessMessage"] = "Ressursen er slettet.";

        return LocalRedirect(Url.Action(nameof(MineRessurser)) + Request.HendelsesQuery());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VurderRessurs(int[] needId, int[] resourceId, bool fraMatch = false)
    {
        if (resourceId.Length == 0 || (needId.Length != 1 && needId.Length != resourceId.Length) ||
            resourceId.Distinct().Count() != resourceId.Length ||
            needId.Distinct().Count() != needId.Length)
            return VisMatchfeil("Velg ett behov eller ett behov per ressurs.", fraMatch);

        var result = await _assignments.RequestAsync(needId, resourceId);
        var errorMessage = result switch
        {
            RequestAssignmentResult.InactiveNeed => "Et valgt behov finnes ikke eller er avsluttet.",
            RequestAssignmentResult.UnavailableResource => "En valgt ressurs er ikke tilgjengelig.",
            RequestAssignmentResult.BusyResource => "En valgt ressurs er allerede under vurdering eller tildelt.",
            _ => null
        };
        if (errorMessage is not null)
            return VisMatchfeil(errorMessage, fraMatch);

        TempData["SuccessMessage"] = resourceId.Length == 1
            ? "Ressursen er sendt til vurdering."
            : "Ressursene er sendt til vurdering.";

        return LocalRedirect(this.OversiktUrl());
    }

    private IActionResult VisMatchfeil(string melding, bool fraMatch)
    {
        TempData["MatchError"] = melding;
        return LocalRedirect(fraMatch
            ? Url.RouteUrl("offentlig-match") + Request.HendelsesQuery()
            : this.OversiktUrl());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TildelRessurs(int assignmentId)
    {
        var assignment = await _assignments.FindAsync(assignmentId);
        var result = await _assignmentService.AcceptAsync(assignment);

        switch (result)
        {
            case AssignmentActionResult.NotFound:
                return NotFound("Ressursvurderingen finnes ikke.");
            case AssignmentActionResult.NotRequested:
                return BadRequest("Ressursen er ikke til vurdering.");
            case AssignmentActionResult.InactiveNeed:
                return BadRequest("Behovet er ikke lenger aktivt.");
            case AssignmentActionResult.UnavailableResource:
                return BadRequest("Ressursen er ikke tilgjengelig.");
            case AssignmentActionResult.BusyResource:
                return BadRequest("Ressursen er allerede opptatt.");
            case AssignmentActionResult.Succeeded:
                break;
            default:
                throw new InvalidOperationException($"Unexpected assignment result: {result}.");
        }

        TempData["SuccessMessage"] = "Ressursen er tildelt.";

        return LocalRedirect(this.OversiktUrl());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FrigiRessurs(int assignmentId)
    {
        var assignment = await _assignments.FindAsync(assignmentId);
        var result = await _assignmentService.ReleaseAsync(assignment);
        if (result == AssignmentActionResult.NotFound)
            return NotFound("Tildelingen finnes ikke.");

        if (result == AssignmentActionResult.NotAssigned)
            return BadRequest("Ressursen er ikke tildelt.");

        TempData["SuccessMessage"] = "Ressursen er frigitt.";

        return LocalRedirect(this.OversiktUrl());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AvslaRessurs(int assignmentId)
    {
        var assignment = await _assignments.FindAsync(assignmentId);
        var result = await _assignmentService.DeclineAsync(assignment);
        if (result == AssignmentActionResult.NotFound)
            return NotFound("Ressursvurderingen finnes ikke.");

        if (result == AssignmentActionResult.NotRequested)
            return BadRequest("Ressursen er ikke til vurdering.");

        TempData["SuccessMessage"] = "Ressursforslaget er avslått.";

        return LocalRedirect(this.OversiktUrl());
    }
    private bool TryHentBrukerId(out int brukerId)
    {
        var tekst = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(tekst, out brukerId);
    }
}
