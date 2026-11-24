using Totalforsvaret.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
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
        return View(new CreateCrisisInput
        {
            StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow,
                "Europe/Oslo")
        });
    }

    // Kontrollerer og lagrer krisen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCrisisInput input)
    {
        if (await _userManager.GetUserAsync(User)
            is not Offentligaktør offentligAktor)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
            return View(input);

        var crisis = new Crisis { OffentligAktorId = offentligAktor.Id };
        ApplyInput(crisis, input);
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

    return View("Create", new CrisisInput
    {
        Name = crisis.Name,
        Description = crisis.Description,
        Area = crisis.Area,
        Type = crisis.Type,
        Severity = crisis.Severity == CrisisSeverity.Unspecified ? null : crisis.Severity,
        StartTime = crisis.StartTime,
        Status = crisis.Status == CrisisStatus.Unspecified ? null : crisis.Status,
        Latitude = (double?)crisis.Latitude,
        Longitude = (double?)crisis.Longitude
    });
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(
    int id,
    CrisisInput input)
{
    var existing = await _crises.FindAsync(id, trackChanges: true);

    if (existing == null)
        return NotFound();

    if (!ModelState.IsValid)
    {
        ViewData["EditId"] = id;
        return View("Create", input);
    }

    ApplyInput(existing, input);
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

    private static void ApplyInput(Crisis crisis, CrisisInput input)
    {
        crisis.Name = input.Name;
        crisis.Description = input.Description;
        crisis.Area = input.Area ?? "";
        crisis.Type = input.Type;
        crisis.Severity = input.Severity!.Value;
        crisis.StartTime = input.StartTime!.Value;
        crisis.Status = input.Status!.Value;
        crisis.Latitude = (decimal?)input.Latitude;
        crisis.Longitude = (decimal?)input.Longitude;
    }
}
