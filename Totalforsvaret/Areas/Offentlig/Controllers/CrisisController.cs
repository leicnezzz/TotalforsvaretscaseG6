using Totalforsvaret.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig"), Authorize(Policy = "KunOffentlig")]



/// <summary>
/// Manages crisis registration, editing, closure and deletion for public actors.
/// </summary>
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

    /// <summary>
    /// Shows crises by severity and then by most recent start time.
    /// </summary>
    [HttpGet("/crisis/overview", Name = "crisis-overview")]
    public async Task<IActionResult> Overview()
    {
        var crises = await _crises.ListOverviewAsync();

        return View(crises);
    }


    /// <summary>
    /// Opens the form with the current local time in Norway as the default start time.
    /// </summary>
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

    /// <summary>
    /// Registers a crisis with a required map position and records the signed-in actor as its
    /// creator.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCrisisInput input)
    {
        if (await _userManager.GetUserAsync(User)
            is not PublicActor publicActor)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
            return View(input);

        var crisis = new Crisis { OffentligAktorId = publicActor.Id };
        ApplyInput(crisis, input);
        await _crises.AddAsync(crisis);

        return LocalRedirect(this.OverviewUrl(openCrisisId: crisis.Id));
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
        Type = crisis.Type == CrisisType.Unspecified ? null : crisis.Type,
        Severity = crisis.Severity == CrisisSeverity.Unspecified ? null : crisis.Severity,
        StartTime = crisis.StartTime,
        Status = crisis.Status == CrisisStatus.Unspecified ? null : crisis.Status,
        Latitude = (double?)crisis.Latitude,
        Longitude = (double?)crisis.Longitude
    });
}

/// <summary>
/// Updates an existing crisis. Any public actor can edit it, and both coordinate fields may
/// be left blank.
/// </summary>
[HttpPost, ValidateAntiForgeryToken]
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

    return LocalRedirect(this.OverviewUrl());
}
/// <summary>
/// Deletes a crisis together with its associated needs and resource assignments.
/// </summary>
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var crisis = await _crises.FindAsync(id, trackChanges: true);

    if (crisis == null)
        return NotFound();

    await _crises.DeleteAsync(crisis);

    return LocalRedirect(this.OverviewUrl());
}
/// <summary>
/// Closes an active crisis and leaves an existing crisis with another status unchanged.
/// </summary>
[HttpPost, ValidateAntiForgeryToken]
[ActionName("Avslutt")]
public async Task<IActionResult> Close(int id)
{
    var finnes = await _crises.ExistsAsync(id);

    if (!finnes)
        return NotFound();

    await _crises.CloseIfActiveAsync(id);

    return LocalRedirect(this.OverviewUrl());
}

    private static void ApplyInput(Crisis crisis, CrisisInput input)
    {
        crisis.Name = input.Name;
        crisis.Description = input.Description;
        crisis.Area = input.Area ?? "";
        crisis.Type = input.Type!.Value;
        crisis.Severity = input.Severity!.Value;
        crisis.StartTime = input.StartTime!.Value;
        crisis.Status = input.Status!.Value;
        crisis.Latitude = (decimal?)input.Latitude;
        crisis.Longitude = (decimal?)input.Longitude;
    }
}
