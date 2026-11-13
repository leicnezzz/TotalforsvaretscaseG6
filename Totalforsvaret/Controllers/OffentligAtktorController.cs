using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Handlers;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
namespace Totalforsvaret.Controllers;

[Authorize(Policy = "KunOffentlig")]
public class OffentligAktorController : Controller
{
    private readonly ResourceHandler _resourceHandler;
    private readonly AppDbContext? _context;

    public OffentligAktorController(
        ResourceHandler resourceHandler,
        AppDbContext? context = null)
    {
        _resourceHandler = resourceHandler;
        _context = context;
    }

    public IActionResult Index([FromQuery] int[]? hendelser = null)
    {
        // Henter tilgjengelige ressurser
        var ressurser = _resourceHandler.GetResources();

        var tilgjengeligeRessurser = ressurser
            .Where(r => r.Available)
            .ToList();

        // Henter antall aktive behov
        ViewBag.AktiveBehov = _context?.Needs.Count(n =>
            n.Status == "Åpen" ||
            n.Status == "Under behandling") ?? 0;
        
        // Henter antall løste behov.
        ViewBag.LosteBehov = _context?.Needs.Count(n =>
            n.Status == "Løst") ?? 0;

        // Henter aktive behov med gyldige kartkoordinater
        ViewBag.BehovPaKart = _context?.Needs
            .Where(n =>
                (n.Status == "Åpen" || n.Status == "Under behandling") &&
                n.Latitude != null &&
                n.Longitude != null)
            .ToList();
        
        // Henter de fem nyeste aktive behovene
        ViewBag.AktiveBehovListe = _context?.Needs
            .Where(n => n.Status == "Åpen" || n.Status == "Under behandling")
            .OrderByDescending(n => n.Id)
            .Take(5)
            .ToList();
        
        // Alle hendelser kan åpnes, også inaktive og hendelser uten eget kartpunkt.
        ViewBag.Kartmarkeringer = _context?.CrisisMapAnnotations.AsNoTracking().ToList()
            ?? new List<Totalforsvaret.Models.CrisisMapAnnotation>();
        ViewBag.KriserPaKart = _context?.Crises.AsNoTracking()
            .Select(c => new
            {
                id = c.Id,
                navn = c.Name,
                type = c.Type,
                alvorlighetsgrad = c.Severity,
                status = c.Status,
                beskrivelse = c.Description,
                latitude = c.Latitude,
                longitude = c.Longitude
            })
            .ToList();
        
        // Alle aktive og løste behov til kortenes lister.
        ViewBag.BehovTilLister = _context?.Needs
            .AsNoTracking()
            .Where(n => n.Status == "Åpen" ||
                        n.Status == "Under behandling" ||
                        n.Status == "Løst")
            .Select(n => new
            {
                id = n.Id,
                navn = n.Name,
                type = n.Type,
                prioritet = n.Priority,
                status = n.Status,
                kriseId = n.CrisisId,
                krise = n.Crisis != null ? n.Crisis.Name : "",
                latitude = n.Latitude,
                longitude = n.Longitude
            })
            .ToList();
       
        // Teller kriser med status Aktiv.
        ViewBag.AktiveKriser = _context?.Set<Totalforsvaret.Models.Crisis>()
            .Count(k => k.Status == "Aktiv") ?? 0;
        var forslag = new List<MatchSuggestionViewModel>();
        var aktiveTildelinger = _context?.ResourceAssignments
            .AsNoTracking()
            .Where(a => a.Status == AssignmentStatus.Requested ||
                        a.Status == AssignmentStatus.Assigned)
            .ToList() ?? new List<ResourceAssignment>();

        ViewBag.AktiveTildelinger = aktiveTildelinger;

        if (_context != null)
        {
            var aktiveBehov = _context.Needs
                .AsNoTracking()
                .Where(n => n.Status == "Åpen" ||
                            n.Status == "Under behandling")
                .ToList();

            var na = DateTime.Now;

            var kandidater = _context.Resources
                .AsNoTracking()
                .Include(r => r.Category)
                .Where(r => r.Available &&
                            (r.TilgjengeligFra == null ||
                             r.TilgjengeligFra <= na))
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
        }

        ViewBag.MatchForslag = forslag
            .OrderBy(f => f.DistanceKm ?? double.MaxValue)
            .ThenBy(f => f.NeedId)
            .ThenBy(f => f.ResourceId)
            .ToList();

        ViewBag.ForeslatteTreff = forslag.Count;
        ViewBag.OpptatteRessursIder = aktiveTildelinger.Select(a => a.ResourceId).ToHashSet();
        
        ViewBag.KriserTilLister = _context?.Crises
            .AsNoTracking()
            .Select(c => new
            {
                id = c.Id,
                navn = c.Name,
                type = c.Type,
                omrade = c.Area,
                alvorlighetsgrad = c.Severity,
                starttidspunkt = c.StartTime,
                status = c.Status,
                beskrivelse = c.Description,
                latitude = c.Latitude,
                longitude = c.Longitude
            })
            .ToList();
 
        // Brukes til å markere egne ressurser på kartet.
        TryHentBrukerId(out var minBrukerId);
        ViewBag.MinBrukerId = minBrukerId;
        
        // Henter de fem siste tildelingene til Koordinering-boksen.
        ViewBag.SisteTildelinger = _context?.ResourceAssignments
            .AsNoTracking()
            .OrderByDescending(a => a.RequestedAtUtc)
            .Take(5)
            .Select(a => new
            {
                behov = _context.Needs
                    .Where(n => n.Id == a.NeedId)
                    .Select(n => n.Name)
                    .FirstOrDefault(),
                ressurs = _context.Resources
                    .Where(r => r.ResourceId == a.ResourceId)
                    .Select(r => r.Object)
                    .FirstOrDefault(),
                status = a.Status,
                tidspunkt = a.RequestedAtUtc
            })
            .ToList();
      
        return View(tilgjengeligeRessurser);
    }
    [HttpGet]
    public IActionResult NyRessurs() => RedirectToRoute("ressursregistrering");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NyRessurs(
        [FromForm(Name = "Object")] string objekt,
        int categoryId,
        string? navn,
        string kontaktpunkt,
        string address,
        decimal latitude,
        decimal longitude,
        string? condition,
        string? additionalInformation)
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        if (!TryHentBrukerId(out var brukerId)) return Challenge();

        var kategoriFinnes = _resourceHandler.GetCategories()
            .Any(c => c.CategoryId == categoryId);

        if (!kategoriFinnes)
        {
            TempData["ErrorMessage"] = "Kategorien finnes ikke.";
            return RedirectToAction(nameof(NyRessurs));
        }

        var ressurs = new Totalforsvaret.Models.resourcemodels.Resource
        {
            Object = objekt.Trim(),
            BrukerId = brukerId,
            CategoryId = categoryId,
            Available = true,
            Navn = navn?.Trim() ?? string.Empty,
            Kontaktpunkt = kontaktpunkt.Trim(),
            Address = address.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            Condition = condition?.Trim() ?? string.Empty,
            AdditionalInformation = additionalInformation?.Trim() ?? string.Empty
        };

        _context.Resources.Add(ressurs);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursen er registrert.";

        return LocalRedirect(this.OversiktUrl());
    }
    [HttpGet]
    public async Task<IActionResult> MineRessurser()
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        if (!TryHentBrukerId(out var brukerId)) return Challenge();

        var ressurser = await _context.Resources
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.BrukerId == brukerId)
            .OrderByDescending(r => r.ResourceId)
            .ToListAsync();

        return View(ressurser);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SlettRessurs(int resourceId)
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        if (!TryHentBrukerId(out var brukerId)) return Challenge();

        // Bare egne ressurser kan slettes.
        var ressurs = await _context.Resources.FirstOrDefaultAsync(r =>
            r.ResourceId == resourceId && r.BrukerId == brukerId);

        if (ressurs == null)
            return NotFound("Ressursen finnes ikke.");

        // Ressurser som har vært i en tildeling beholdes på grunn av historikken.
        var harTildeling = await _context.ResourceAssignments
            .AnyAsync(a => a.ResourceId == resourceId);

        if (harTildeling)
        {
            TempData["ErrorMessage"] = "Ressursen har vært brukt i en tildeling og kan ikke slettes.";
            return RedirectToAction(nameof(MineRessurser));
        }

        _context.Resources.Remove(ressurs);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursen er slettet.";

        return RedirectToAction(nameof(MineRessurser));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VurderRessurs(int[] needId, int[] resourceId)
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        if (resourceId.Length == 0 || (needId.Length != 1 && needId.Length != resourceId.Length) ||
            resourceId.Distinct().Count() != resourceId.Length ||
            needId.Distinct().Count() != needId.Length)
            return BadRequest("Velg ett behov eller ett behov per ressurs.");

        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        var aktiveBehov = await _context.Needs.CountAsync(n =>
            Enumerable.Contains(needId, n.Id) &&
            (n.Status == "Åpen" || n.Status == "Under behandling"));

        if (aktiveBehov != needId.Length)
            return BadRequest("Et valgt behov finnes ikke eller er avsluttet.");

        var tilgjengelige = await _context.Resources.CountAsync(r =>
            Enumerable.Contains(resourceId, r.ResourceId) &&
            r.Available &&
            (r.TilgjengeligFra == null || r.TilgjengeligFra <= DateTime.Now));

        if (tilgjengelige != resourceId.Length)
            return BadRequest("En valgt ressurs er ikke tilgjengelig.");

        var opptatt = await _context.ResourceAssignments.AnyAsync(a =>
            Enumerable.Contains(resourceId, a.ResourceId) &&
            (a.Status == AssignmentStatus.Requested || a.Status == AssignmentStatus.Assigned));

        if (opptatt)
            return BadRequest("En valgt ressurs er allerede under vurdering eller tildelt.");

        var tidspunkt = DateTime.UtcNow;
        _context.ResourceAssignments.AddRange(resourceId.Select((id, index) => new ResourceAssignment
        {
            NeedId = needId.Length == 1 ? needId[0] : needId[index],
            ResourceId = id,
            Status = AssignmentStatus.Requested,
            RequestedAtUtc = tidspunkt
        }));
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = resourceId.Length == 1
            ? "Ressursen er sendt til vurdering."
            : "Ressursene er sendt til vurdering.";

        return LocalRedirect(this.OversiktUrl());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TildelRessurs(int assignmentId)
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        // Finn vurderingen som skal godkjennes.
        var tildeling = await _context.ResourceAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (tildeling == null)
            return NotFound("Ressursvurderingen finnes ikke.");

        // Bare ressurser til vurdering kan tildeles.
        if (tildeling.Status != AssignmentStatus.Requested)
            return BadRequest("Ressursen er ikke til vurdering.");

        // Behovet må fortsatt være aktivt.
        var aktivtBehov = await _context.Needs.AnyAsync(n =>
            n.Id == tildeling.NeedId &&
            (n.Status == "Åpen" || n.Status == "Under behandling"));

        if (!aktivtBehov)
            return BadRequest("Behovet er ikke lenger aktivt.");

        // Ressursen må fortsatt være tilgjengelig.
        var tilgjengeligRessurs = await _context.Resources.AnyAsync(r =>
            r.ResourceId == tildeling.ResourceId &&
            r.Available &&
            (r.TilgjengeligFra == null ||
             r.TilgjengeligFra <= DateTime.Now));

        if (!tilgjengeligRessurs)
            return BadRequest("Ressursen er ikke tilgjengelig.");

        // Hindre tildeling dersom ressursen allerede er tildelt.
        var alleredeTildelt = await _context.ResourceAssignments
            .AnyAsync(a =>
                a.ResourceId == tildeling.ResourceId &&
                a.Id != tildeling.Id &&
                (a.Status == AssignmentStatus.Requested ||
                 a.Status == AssignmentStatus.Assigned));

        if (alleredeTildelt)
            return BadRequest("Ressursen er allerede opptatt.");

        tildeling.Status = AssignmentStatus.Assigned;
        tildeling.AssignedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursen er tildelt.";

        return LocalRedirect(this.OversiktUrl());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FrigiRessurs(int assignmentId)
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        // Finn den eksisterende tildelingen.
        var tildeling = await _context.ResourceAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (tildeling == null)
            return NotFound("Tildelingen finnes ikke.");

        // Bare tildelte ressurser kan frigis.
        if (tildeling.Status != AssignmentStatus.Assigned)
            return BadRequest("Ressursen er ikke tildelt.");

        // Avslutt tildelingen og behold historikken.
        tildeling.Status = AssignmentStatus.Released;
        tildeling.ClosedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursen er frigitt.";

        return LocalRedirect(this.OversiktUrl());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AvslaRessurs(int assignmentId)
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        var tildeling = await _context.ResourceAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (tildeling == null)
            return NotFound("Ressursvurderingen finnes ikke.");

        // Bare vurderinger kan avslås.
        if (tildeling.Status != AssignmentStatus.Requested)
            return BadRequest("Ressursen er ikke til vurdering.");

        // Avslutt vurderingen, men behold historikken.
        tildeling.Status = AssignmentStatus.Rejected;
        tildeling.ClosedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursforslaget er avslått.";

        return LocalRedirect(this.OversiktUrl());
    }
    private bool TryHentBrukerId(out int brukerId)
    {
        var tekst = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(tekst, out brukerId);
    }
    
    private static double? BeregnAvstandKm(
        double? behovLatitude,
        double? behovLongitude,
        decimal ressursLatitude,
        decimal ressursLongitude)
    {
        if (!behovLatitude.HasValue || !behovLongitude.HasValue)
            return null;
        
        var lat1 = behovLatitude.Value;
        var lon1 = behovLongitude.Value;
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
