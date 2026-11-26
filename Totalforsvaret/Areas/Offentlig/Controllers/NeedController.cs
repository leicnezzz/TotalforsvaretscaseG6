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

/// <summary>
/// Manages needs used by the public actor workflows.
/// </summary>
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

    /// <summary>
    /// Opens the need form using the crises selected on the overview, if any.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? crisisId = null)
    {
        var openCrisisIds = GetOpenCrisisIds();
        var viewModel = new NeedCreateViewModel
        {
            Crises = await GetCrisesAsync(openCrisisIds),
            CrisisLocations = await _crises.ListMapPointsAsync(openCrisisIds),
            CrisisId = openCrisisIds.Count == 1 ? openCrisisIds[0] :
                openCrisisIds.Count == 0 ? crisisId : null,
            Status = NeedStatus.InProgress
        };

        return View(viewModel);
    }

    /// <summary>
    /// Registers a need for the signed-in public actor and checks the selected crisis against the
    /// form's options.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NeedCreateViewModel viewModel)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is not PublicActor publicActor)
            return Forbid();

        var openCrisisIds = GetOpenCrisisIds();
        if (viewModel.CrisisId is int crisisId &&
            ((openCrisisIds.Count > 0 && !openCrisisIds.Contains(crisisId)) ||
             !await _crises.ExistsAsync(crisisId)))
        {
            ModelState.AddModelError(
                nameof(viewModel.CrisisId),
                "Velg en gyldig krise.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Crises = await GetCrisesAsync(openCrisisIds);
            viewModel.CrisisLocations = await _crises.ListMapPointsAsync(openCrisisIds);
            return View(viewModel);
        }

        var need = new Need
        {
            Name = viewModel.Name.Trim(),
            Type = viewModel.Type!.Value,
            Description = viewModel.Description.Trim(),
            Priority = viewModel.Priority!.Value,
            Status = viewModel.Status!.Value,
            Latitude = viewModel.Latitude!.Value,
            Longitude = viewModel.Longitude!.Value,
            CrisisId = viewModel.CrisisId,
            OffentligAktorId = publicActor.Id
        };

        await _needs.AddAsync(need);

        return LocalRedirect(this.OverviewUrl(viewModel.CrisisId));
    }

    private List<int> GetOpenCrisisIds() => Request.Query["hendelser"]
        .Select(value => int.TryParse(value, out var id) ? id : 0)
        .Where(id => id > 0)
        .Distinct()
        .ToList();

    private async Task<IEnumerable<SelectListItem>> GetCrisesAsync(List<int>? openCrisisIds = null)
    {
        var options = await _crises.ListOptionsAsync(openCrisisIds);
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
            Type = need.Type == NeedType.Unspecified ? null : need.Type,
            Description = need.Description,
            Priority = need.Priority == NeedPriority.Unspecified ? null : need.Priority,
            Status = need.Status == NeedStatus.Unspecified ? null : need.Status,
            CrisisId = need.CrisisId,
            Latitude = need.Latitude,
            Longitude = need.Longitude,
            Crises = await GetCrisesAsync()
        };

        return View("Create", viewModel);
    }

    /// <summary>
    /// Updates a need after validating its coordinates and checking that the selected crisis
    /// exists.
    /// </summary>
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

        if (!ModelState.IsValid)
        {
            ViewData["EditId"] = id;
            viewModel.Crises = await GetCrisesAsync();
            return View("Create", viewModel);
        }

        need.Name = viewModel.Name.Trim();
        need.Type = viewModel.Type!.Value;
        need.Description = viewModel.Description.Trim();
        need.Priority = viewModel.Priority!.Value;
        need.Status = viewModel.Status!.Value;
        need.CrisisId = viewModel.CrisisId;
        need.Latitude = viewModel.Latitude;
        need.Longitude = viewModel.Longitude;

        await _needs.UpdateAsync(need);

        return LocalRedirect(this.OverviewUrl());
    }

    /// <summary>
    /// Deletes a need and its resource assignments together.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var need = await _needs.FindAsync(id, trackChanges: true);

        if (need == null)
            return NotFound();

        await _needs.DeleteAsync(need);

        return LocalRedirect(this.OverviewUrl());
    }
}
