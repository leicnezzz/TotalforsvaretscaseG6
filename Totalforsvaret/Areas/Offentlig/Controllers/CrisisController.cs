using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using Microsoft.EntityFrameworkCore;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]


public class CrisisController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<Bruker> _userManager;

    public CrisisController(
        AppDbContext context,
        UserManager<Bruker> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Åpner kriseskjemaet.
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Crisis
        {
            StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow,
                "Europe/Oslo")
        });
    }

    // Kontrollerer og lagrer krisen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Area,Type,Severity,StartTime,Status,Latitude,Longitude")]
        Crisis crisis)
    {
        // Henter den innloggede brukeren.
        var bruker = await _userManager.GetUserAsync(User);

        // Kontrollerer at brukeren er en offentlig aktør.
        //if (bruker is not Offentligaktør offentligAktor)
        //    return Forbid();

        // Fjerner mellomrom før og etter teksten.
        crisis.Name = crisis.Name?.Trim() ?? "";
        crisis.Description = crisis.Description?.Trim() ?? "";
        crisis.Area = crisis.Area?.Trim() ?? "";
        crisis.Type = crisis.Type?.Trim() ?? "";
        crisis.Severity = crisis.Severity?.Trim() ?? "";
        crisis.Status = crisis.Status?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(crisis.Name) ||
            crisis.Name.Length > 100)
            ModelState.AddModelError(
                nameof(crisis.Name),
                "Navnet må fylles ut og kan ha maksimalt 100 tegn.");

        if (string.IsNullOrWhiteSpace(crisis.Description) ||
            crisis.Description.Length > 1500)
            ModelState.AddModelError(
                nameof(crisis.Description),
                "Beskrivelsen må fylles ut og kan ha maksimalt 1500 tegn.");

        if (string.IsNullOrWhiteSpace(crisis.Area) ||
            crisis.Area.Length > 100)
            ModelState.AddModelError(
                nameof(crisis.Area),
                "Området må fylles ut og kan ha maksimalt 100 tegn.");

        var gyldigeTyper = new[]
        {
            "Flom", "Brann", "Strømbrudd", "Ekstremvær", "Annet"
        };

        if (!gyldigeTyper.Contains(crisis.Type))
            ModelState.AddModelError(
                nameof(crisis.Type), "Velg en gyldig krisetype.");

        var gyldigeAlvorlighetsgrader = new[]
        {
            "Lav", "Middels", "Høy", "Kritisk"
        };

        if (!gyldigeAlvorlighetsgrader.Contains(crisis.Severity))
            ModelState.AddModelError(
                nameof(crisis.Severity),
                "Velg en gyldig alvorlighetsgrad.");

        if (crisis.StartTime == default)
            ModelState.AddModelError(
                nameof(crisis.StartTime),
                "Velg et starttidspunkt.");
        // Kontrollerer at kartkoordinatene er gyldige.
        if (crisis.Latitude.HasValue &&
            (crisis.Latitude < -90 || crisis.Latitude > 90))
        {
            ModelState.AddModelError(
                nameof(crisis.Latitude),
                "Ugyldig breddegrad.");
        }

        if (crisis.Longitude.HasValue &&
            (crisis.Longitude < -180 || crisis.Longitude > 180))
        {
            ModelState.AddModelError(
                nameof(crisis.Longitude),
                "Ugyldig lengdegrad.");
        }

        var gyldigeStatuser = new[]
        {
            "Aktiv", "Under kontroll", "Avsluttet"
        };

        if (!gyldigeStatuser.Contains(crisis.Status))
            ModelState.AddModelError(
                nameof(crisis.Status), "Velg en gyldig status.");

        // Viser skjemaet igjen hvis noe er ugyldig.
        if (!ModelState.IsValid)
            return View(crisis);

        // Kobler krisen til aktøren som er logget inn.
        // crisis.OffentligAktorId = offentligAktor.Id;

        _context.Crises.Add(crisis);
        await _context.SaveChangesAsync();

        return RedirectToAction(
            "Index", "OffentligAktor", new { area = "" });
    }
    [HttpGet]
public async Task<IActionResult> Edit(int id)
{
    var crisis = await _context.Crises
        .AsNoTracking()
        .SingleOrDefaultAsync(c => c.Id == id);

    if (crisis == null)
        return NotFound();

    ViewData["EditId"] = id;

    return View("Create", crisis);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(
    int id,
    [Bind("Name,Description,Area,Type,Severity,StartTime,Status,Latitude,Longitude")]
    Crisis crisis)
{
    var existing = await _context.Crises
        .SingleOrDefaultAsync(c => c.Id == id);

    if (existing == null)
        return NotFound();

    crisis.Name = crisis.Name?.Trim() ?? "";
    crisis.Description = crisis.Description?.Trim() ?? "";
    crisis.Area = crisis.Area?.Trim() ?? "";
    crisis.Type = crisis.Type?.Trim() ?? "";
    crisis.Severity = crisis.Severity?.Trim() ?? "";
    crisis.Status = crisis.Status?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(crisis.Name) ||
        crisis.Name.Length > 100)
    {
        ModelState.AddModelError(
            nameof(crisis.Name),
            "Navnet må fylles ut og kan ha maksimalt 100 tegn.");
    }

    if (string.IsNullOrWhiteSpace(crisis.Description) ||
        crisis.Description.Length > 1500)
    {
        ModelState.AddModelError(
            nameof(crisis.Description),
            "Beskrivelsen må fylles ut og kan ha maksimalt 1500 tegn.");
    }

    if (string.IsNullOrWhiteSpace(crisis.Area) ||
        crisis.Area.Length > 100)
    {
        ModelState.AddModelError(
            nameof(crisis.Area),
            "Området må fylles ut og kan ha maksimalt 100 tegn.");
    }

    var gyldigeTyper = new[]
    {
        "Flom", "Brann", "Strømbrudd", "Ekstremvær", "Annet"
    };

    if (!gyldigeTyper.Contains(crisis.Type))
    {
        ModelState.AddModelError(
            nameof(crisis.Type),
            "Velg en gyldig krisetype.");
    }

    var gyldigeAlvorlighetsgrader = new[]
    {
        "Lav", "Middels", "Høy", "Kritisk"
    };

    if (!gyldigeAlvorlighetsgrader.Contains(crisis.Severity))
    {
        ModelState.AddModelError(
            nameof(crisis.Severity),
            "Velg en gyldig alvorlighetsgrad.");
    }

    var gyldigeStatuser = new[]
    {
        "Aktiv", "Under kontroll", "Avsluttet"
    };

    if (!gyldigeStatuser.Contains(crisis.Status))
    {
        ModelState.AddModelError(
            nameof(crisis.Status),
            "Velg en gyldig status.");
    }

    if (crisis.StartTime == default)
    {
        ModelState.AddModelError(
            nameof(crisis.StartTime),
            "Velg et starttidspunkt.");
    }

    if (crisis.Latitude.HasValue &&
        (double.IsNaN(crisis.Latitude.Value) ||
         crisis.Latitude < -90 ||
         crisis.Latitude > 90))
    {
        ModelState.AddModelError(
            nameof(crisis.Latitude),
            "Ugyldig breddegrad.");
    }

    if (crisis.Longitude.HasValue &&
        (double.IsNaN(crisis.Longitude.Value) ||
         crisis.Longitude < -180 ||
         crisis.Longitude > 180))
    {
        ModelState.AddModelError(
            nameof(crisis.Longitude),
            "Ugyldig lengdegrad.");
    }

    if (!ModelState.IsValid)
    {
        ViewData["EditId"] = id;
        return View("Create", crisis);
    }

    existing.Name = crisis.Name;
    existing.Description = crisis.Description;
    existing.Area = crisis.Area;
    existing.Type = crisis.Type;
    existing.Severity = crisis.Severity;
    existing.StartTime = crisis.StartTime;
    existing.Status = crisis.Status;
    existing.Latitude = crisis.Latitude;
    existing.Longitude = crisis.Longitude;

    await _context.SaveChangesAsync();

    return RedirectToAction(
        "Index", "OffentligAktor", new { area = "" });
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var crisis = await _context.Crises
        .SingleOrDefaultAsync(c => c.Id == id);

    if (crisis == null)
        return NotFound();

    // Henter alle behov som tilhører krisen.
    var needs = await _context.Needs
        .Where(n => n.CrisisId == id)
        .ToListAsync();

    // Sletter behovene og deretter selve krisen.
    _context.Needs.RemoveRange(needs);
    _context.Crises.Remove(crisis);

    // Én lagring gjør slettingen samlet i en transaksjon.
    await _context.SaveChangesAsync();

    return RedirectToAction(
        "Index",
        "OffentligAktor",
        new { area = "" });
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Avslutt(int id)
{
    var finnes = await _context.Crises
        .AnyAsync(c => c.Id == id);

    if (!finnes)
        return NotFound();

    // Endrer bare status og tåler gjentatte innsendinger.
    await _context.Crises
        .Where(c => c.Id == id && c.Status == "Aktiv")
        .ExecuteUpdateAsync(oppdatering => oppdatering
            .SetProperty(c => c.Status, "Avsluttet"));

    return RedirectToAction(
        "Index", "OffentligAktor", new { area = "" });
}
}
