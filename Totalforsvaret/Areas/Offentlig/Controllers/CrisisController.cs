using Totalforsvaret.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]



public class CrisisController : Controller
{
    private readonly ICrisisRepository _crises;
    private readonly UserManager<Bruker> _userManager;

    public CrisisController(
        ICrisisRepository crises,
        UserManager<Bruker> userManager)
    {
        _crises = crises;
        _userManager = userManager;
    }

    [HttpGet("/crisis/overview", Name = "crisis-overview")]
    public async Task<IActionResult> Overview()
    {
        var crises = await _crises.ListOverviewAsync();

        return View(crises);
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
        if (await _userManager.GetUserAsync(User)
            is not Offentligaktør offentligAktor)
        {
            return Forbid();
        }

        // Fjerner mellomrom før og etter teksten.
        NormalizeCrisis(crisis);

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

        if (crisis.Area.Length > 100)
            ModelState.AddModelError(
                nameof(crisis.Area),
                "Områdedetaljene kan ha maksimalt 100 tegn.");

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
        if (crisis.Latitude is not double latitude || !double.IsFinite(latitude) || Math.Abs(latitude) > 90 ||
            crisis.Longitude is not double longitude || !double.IsFinite(longitude) || Math.Abs(longitude) > 180)
        {
            ModelState.AddModelError(nameof(crisis.Latitude), "Velg en gyldig plassering på kartet.");
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
        crisis.OffentligAktorId = offentligAktor.Id;

        await _crises.AddAsync(crisis);

        return LocalRedirect(this.OversiktUrl(openCrisisId: crisis.Id));
    }
    [HttpGet]
public async Task<IActionResult> Edit(int id)
{
    var crisis = await _crises.FindAsync(id);

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
    var existing = await _crises.FindAsync(id, trackChanges: true);

    if (existing == null)
        return NotFound();

    NormalizeCrisis(crisis);

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

    if (crisis.Area.Length > 100)
    {
        ModelState.AddModelError(
            nameof(crisis.Area),
            "Områdedetaljene kan ha maksimalt 100 tegn.");
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

    await _crises.SaveChangesAsync();

    return LocalRedirect(this.OversiktUrl());
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var crisis = await _crises.FindAsync(id, trackChanges: true);

    if (crisis == null)
        return NotFound();

    await _crises.DeleteAsync(crisis);

    return LocalRedirect(this.OversiktUrl());
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Avslutt(int id)
{
    var finnes = await _crises.ExistsAsync(id);

    if (!finnes)
        return NotFound();

    // Endrer bare status og tåler gjentatte innsendinger.
    await _crises.CloseIfActiveAsync(id);

    return LocalRedirect(this.OversiktUrl());
}

    private static void NormalizeCrisis(Crisis crisis)
    {
        crisis.Name = crisis.Name?.Trim() ?? "";
        crisis.Description = crisis.Description?.Trim() ?? "";
        crisis.Area = crisis.Area?.Trim() ?? "";
        crisis.Type = crisis.Type?.Trim() ?? "";
        crisis.Severity = crisis.Severity?.Trim() ?? "";
        crisis.Status = crisis.Status?.Trim() ?? "";
    }
}
