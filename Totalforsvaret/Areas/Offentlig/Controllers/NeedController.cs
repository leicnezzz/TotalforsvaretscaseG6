using Totalforsvaret.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;


[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]

public class NeedController : Controller
{
    private readonly INeedRepository _needs;
    private readonly ICrisisRepository _crises;
    private readonly UserManager<Bruker> _userManager;

    public NeedController(
        INeedRepository needs,
        ICrisisRepository crises,
        UserManager<Bruker> userManager)
    {
        _needs = needs;
        _crises = crises;
        _userManager = userManager;
    }

    // Åpner skjemaet og henter eksisterende kriser.
    [HttpGet]
    public async Task<IActionResult> Create(int? crisisId = null)
    {
        var apneHendelser = HentApneHendelser();
        var viewModel = new NeedCreateViewModel
        {
            Crises = await GetCrisesAsync(apneHendelser),
            CrisisLocations = await _crises.ListMapPointsAsync(apneHendelser),
            CrisisId = apneHendelser.Count == 1 ? apneHendelser[0] :
                apneHendelser.Count == 0 ? crisisId : null,
            Status = NeedStatus.InProgress
        };

        return View(viewModel);
    }

    // Kontrollerer og lagrer behovet.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NeedCreateViewModel viewModel)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is not Offentligaktør offentligAktor)
            return Forbid();

        var apneHendelser = HentApneHendelser();
        if (viewModel.CrisisId is int crisisId &&
            ((apneHendelser.Count > 0 && !apneHendelser.Contains(crisisId)) ||
             !await _crises.ExistsAsync(crisisId)))
        {
            ModelState.AddModelError(
                nameof(viewModel.CrisisId),
                "Velg en gyldig krise.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Crises = await GetCrisesAsync(apneHendelser);
            viewModel.CrisisLocations = await _crises.ListMapPointsAsync(apneHendelser);
            return View(viewModel);
        }

        // Oppretter behovet med riktig aktørtilknytning.
        var need = new Need
        {
            Name = viewModel.Name.Trim(),
            Type = viewModel.Type.Trim(),
            Description = viewModel.Description.Trim(),
            Priority = viewModel.Priority,
            Status = viewModel.Status!.Value,
            Latitude = viewModel.Latitude!.Value,
            Longitude = viewModel.Longitude!.Value,
            CrisisId = viewModel.CrisisId,
            OffentligAktorId = offentligAktor.Id
        };

        await _needs.AddAsync(need);

        return LocalRedirect(this.OversiktUrl(viewModel.CrisisId));
    }

    // Henter krisene som kan velges i skjemaet.
    private List<int> HentApneHendelser() => Request.Query["hendelser"]
        .Select(value => int.TryParse(value, out var id) ? id : 0)
        .Where(id => id > 0)
        .Distinct()
        .ToList();

    private async Task<IEnumerable<SelectListItem>> GetCrisesAsync(List<int>? apneHendelser = null)
    {
        var options = await _crises.ListOptionsAsync(apneHendelser);
        return options.Select(option => new SelectListItem(option.Name, option.Id.ToString())).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var need = await _needs.FindAsync(id);

        if (need == null)
            return NotFound();

        ViewData["EditId"] = id;

        var viewModel = new NeedCreateViewModel
        {
            Name = need.Name,
            Type = need.Type,
            Description = need.Description,
            Priority = need.Priority,
            Status = need.Status == NeedStatus.Unspecified ? null : need.Status,
            CrisisId = need.CrisisId,
            Latitude = need.Latitude,
            Longitude = need.Longitude,
            Crises = await GetCrisesAsync()
        };

        return View("Create", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        NeedCreateViewModel viewModel)
    {
        var need = await _needs.FindAsync(id, trackChanges: true);

        if (need == null)
            return NotFound();

        if (viewModel.CrisisId is int crisisId &&
            !await _crises.ExistsAsync(crisisId))
        {
            ModelState.AddModelError(
                nameof(viewModel.CrisisId),
                "Velg en gyldig krise.");
        }

        if (viewModel.Latitude.HasValue &&
            (double.IsNaN(viewModel.Latitude.Value) ||
             viewModel.Latitude < -90 ||
             viewModel.Latitude > 90))
        {
            ModelState.AddModelError(
                nameof(viewModel.Latitude),
                "Ugyldig breddegrad.");
        }

        if (viewModel.Longitude.HasValue &&
            (double.IsNaN(viewModel.Longitude.Value) ||
             viewModel.Longitude < -180 ||
             viewModel.Longitude > 180))
        {
            ModelState.AddModelError(
                nameof(viewModel.Longitude),
                "Ugyldig lengdegrad.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["EditId"] = id;
            viewModel.Crises = await GetCrisesAsync();
            return View("Create", viewModel);
        }

        need.Name = viewModel.Name.Trim();
        need.Type = viewModel.Type.Trim();
        need.Description = viewModel.Description.Trim();
        need.Priority = viewModel.Priority;
        need.Status = viewModel.Status!.Value;
        need.CrisisId = viewModel.CrisisId;
        need.Latitude = viewModel.Latitude;
        need.Longitude = viewModel.Longitude;

        await _needs.SaveChangesAsync();

        return LocalRedirect(this.OversiktUrl());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var need = await _needs.FindAsync(id, trackChanges: true);

        if (need == null)
            return NotFound();

        await _needs.DeleteAsync(need);

        return LocalRedirect(this.OversiktUrl());
    }
}
