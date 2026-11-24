using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
namespace Totalforsvaret.Controllers;

[Authorize(Policy = "KunOffentlig")]
public class OffentligAktorController : Controller
{
    private readonly IResourceRepository _resources;
    private readonly IResourceAssignmentRepository _assignments;
    private readonly IPublicOverviewRepository _overview;

    public OffentligAktorController(
        IResourceRepository resources,
        IResourceAssignmentRepository assignments,
        IPublicOverviewRepository overview)
    {
        _resources = resources;
        _assignments = assignments;
        _overview = overview;
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
                type = c.Type,
                alvorlighetsgrad = c.Severity,
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
        var forslag = new List<MatchSuggestionViewModel>();
        var aktiveTildelinger = _overview.ListActiveAssignments();

        ViewBag.AktiveTildelinger = aktiveTildelinger;

		var avsluttedeVurderinger = _overview.ListClosedReviewsForActiveNeeds();

		var treffStatuser = aktiveTildelinger
			.OrderByDescending(a => a.Id)
			.Concat(avsluttedeVurderinger.OrderByDescending(a => a.Id))
			.GroupBy(a => (a.NeedId, a.ResourceId))
			.ToDictionary(gruppe => gruppe.Key, gruppe => gruppe.First().Status);

		ViewBag.TreffStatuser = treffStatuser;

        var aktiveBehov = _overview.ListActiveNeeds();

        var na = DateTime.Now;

        var kandidater = tilgjengeligeRessurser
            .Where(r => r.TilgjengeligFra == null || r.TilgjengeligFra <= na)
            .ToList();

        foreach (var behov in aktiveBehov)
        {
            var kategori = behov.Type switch
            {
                "Transport" => "Vehicles",
                "Utstyr" => "Equipment",
                _ => null
            };

            if (kategori == null)
                continue;

            foreach (var ressurs in kandidater.Where(
                         r => r.Category.Name == kategori))
            {
                forslag.Add(new MatchSuggestionViewModel
                {
                    NeedId = behov.Id,
                    NeedName = behov.Name,
                    ResourceId = ressurs.ResourceId,
                    ResourceName = ressurs.Object,
                    DistanceKm = BeregnAvstandKm(
                        behov.Latitude,
                        behov.Longitude,
                        ressurs.Latitude,
                        ressurs.Longitude)
                });
            }
        }

		ViewBag.MatchForslag = forslag
	 .OrderBy(f =>
	 {
		 if (!treffStatuser.TryGetValue((f.NeedId, f.ResourceId), out var status))
			 return 0; // Ikke vurdert

		 return status == AssignmentStatus.Requested ? 1 : 2;
	 })
	 .ThenBy(f => f.DistanceKm ?? double.MaxValue)
	 .ThenBy(f => f.NeedId)
	 .ThenBy(f => f.ResourceId)
	 .ToList();

		ViewBag.ForeslatteTreff = forslag.Count;
        ViewBag.OpptatteRessursIder = aktiveTildelinger.Select(a => a.ResourceId).ToHashSet();
        
        ViewBag.KriserTilLister = kriser.Select(c => new
            {
                id = c.Id,
                navn = c.Name,
                type = c.Type,
                omrade = c.Area,
                alvorlighetsgrad = c.Severity,
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
        // Finn vurderingen som skal godkjennes.
        var tildeling = await _assignments.FindAsync(assignmentId);

        if (tildeling == null)
            return NotFound("Ressursvurderingen finnes ikke.");

        // Bare ressurser til vurdering kan tildeles.
        if (tildeling.Status != AssignmentStatus.Requested)
            return BadRequest("Ressursen er ikke til vurdering.");

        // Behovet må fortsatt være aktivt.
        var aktivtBehov = await _assignments.HasActiveNeedAsync(tildeling.NeedId);

        if (!aktivtBehov)
            return BadRequest("Behovet er ikke lenger aktivt.");

        // Ressursen må fortsatt være tilgjengelig.
        var tilgjengeligRessurs = await _assignments.HasAvailableResourceAsync(tildeling.ResourceId);

        if (!tilgjengeligRessurs)
            return BadRequest("Ressursen er ikke tilgjengelig.");

        // Hindre tildeling dersom ressursen allerede er tildelt.
        var alleredeTildelt = await _assignments.HasOtherActiveAssignmentAsync(tildeling.Id, tildeling.ResourceId);

        if (alleredeTildelt)
            return BadRequest("Ressursen er allerede opptatt.");

        tildeling.MarkAssigned();

        await _assignments.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursen er tildelt.";

        return LocalRedirect(this.OversiktUrl());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FrigiRessurs(int assignmentId)
    {
        // Finn den eksisterende tildelingen.
        var tildeling = await _assignments.FindAsync(assignmentId);

        if (tildeling == null)
            return NotFound("Tildelingen finnes ikke.");

        // Bare tildelte ressurser kan frigis.
        if (tildeling.Status != AssignmentStatus.Assigned)
            return BadRequest("Ressursen er ikke tildelt.");

        // Avslutt tildelingen og behold historikken.
        tildeling.MarkReleased();

        await _assignments.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursen er frigitt.";

        return LocalRedirect(this.OversiktUrl());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AvslaRessurs(int assignmentId)
    {
        var tildeling = await _assignments.FindAsync(assignmentId);

        if (tildeling == null)
            return NotFound("Ressursvurderingen finnes ikke.");

        // Bare vurderinger kan avslås.
        if (tildeling.Status != AssignmentStatus.Requested)
            return BadRequest("Ressursen er ikke til vurdering.");

        // Avslutt vurderingen, men behold historikken.
        tildeling.MarkRejected();

        await _assignments.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursforslaget er avslått.";

        return LocalRedirect(this.OversiktUrl());
    }
    private bool TryHentBrukerId(out int brukerId)
    {
        var tekst = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(tekst, out brukerId);
    }
    
    private static double? BeregnAvstandKm(
        decimal? behovLatitude,
        decimal? behovLongitude,
        decimal ressursLatitude,
        decimal ressursLongitude)
    {
        if (!behovLatitude.HasValue || !behovLongitude.HasValue)
            return null;
        
        var lat1 = (double)behovLatitude.Value;
        var lon1 = (double)behovLongitude.Value;
        var lat2 = (double)ressursLatitude;
        var lon2 = (double)ressursLongitude;

        if (!double.IsFinite(lat1) || !double.IsFinite(lon1) ||
            lat1 < -90 || lat1 > 90 ||
            lon1 < -180 || lon1 > 180 ||
            lat2 < -90 || lat2 > 90 ||
            lon2 < -180 || lon2 > 180)
            return null;

        const double jordradiusKm = 6371;
        const double radianer = Math.PI / 180;

        var deltaLat = (lat2 - lat1) * radianer;
        var deltaLon = (lon2 - lon1) * radianer;

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                Math.Cos(lat1 * radianer) *
                Math.Cos(lat2 * radianer) *
                Math.Pow(Math.Sin(deltaLon / 2), 2);

        return jordradiusKm * 2 *
               Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
