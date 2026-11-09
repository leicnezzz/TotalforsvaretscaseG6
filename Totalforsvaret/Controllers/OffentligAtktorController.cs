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

    public IActionResult Index()
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
        
        // Henter aktive kriser med kartkoordinater.
        ViewBag.KriserPaKart = _context?.Crises
            .AsNoTracking()
            .Where(c => c.Status == "Aktiv" &&
                        c.Latitude != null &&
                        c.Longitude != null)
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
        
        ViewBag.KriserTilLister = _context?.Crises
            .AsNoTracking()
            .Where(c => c.Status == "Aktiv")
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

        return View(tilgjengeligeRessurser);
    }
    [HttpGet]
    public IActionResult NyRessurs()
    {
        ViewBag.Kategorier = _resourceHandler.GetCategories();
        return View();
    }
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

        var brukerIdTekst = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(brukerIdTekst, out var brukerId))
            return Challenge();

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

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VurderRessurs(int needId, int resourceId)
    {
        if (_context == null)
            return StatusCode(500, "Databasen er ikke tilgjengelig.");

        // Kontroller at behovet fortsatt er aktivt.
        var behovFinnes = await _context.Needs.AnyAsync(n =>
            n.Id == needId &&
            (n.Status == "Åpen" || n.Status == "Under behandling"));

        if (!behovFinnes)
            return BadRequest("Behovet finnes ikke eller er avsluttet.");

        // Kontroller at ressursen er tilgjengelig.
        var ressursFinnes = await _context.Resources.AnyAsync(r =>
            r.ResourceId == resourceId &&
            r.Available &&
            (r.TilgjengeligFra == null ||
             r.TilgjengeligFra <= DateTime.Now));

        if (!ressursFinnes)
            return BadRequest("Ressursen er ikke tilgjengelig.");

        // Ressursen må ikke allerede være under vurdering eller tildelt.
        var opptatt = await _context.ResourceAssignments.AnyAsync(a =>
            a.ResourceId == resourceId &&
            (a.Status == AssignmentStatus.Requested ||
             a.Status == AssignmentStatus.Assigned));

        if (opptatt)
            return BadRequest("Ressursen er allerede under vurdering eller tildelt.");

        var tildeling = new ResourceAssignment
        {
            NeedId = needId,
            ResourceId = resourceId,
            Status = AssignmentStatus.Requested,
            RequestedAtUtc = DateTime.UtcNow
        };

        _context.ResourceAssignments.Add(tildeling);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ressursen er sendt til vurdering.";

        return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
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
