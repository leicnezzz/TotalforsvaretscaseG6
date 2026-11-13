using Totalforsvaret.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;


[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]

public class NeedController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<Bruker> _userManager;

    public NeedController(
        AppDbContext context,
        UserManager<Bruker> userManager)
    {
        _context = context;
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
            CrisisId = apneHendelser.Count == 1 ? apneHendelser[0] :
                apneHendelser.Count == 0 ? crisisId : null,
            Status = "Åpen"
        };

        return View(viewModel);
    }

    // Kontrollerer og lagrer behovet.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NeedCreateViewModel viewModel)
    {
        // var bruker = await _userManager.GetUserAsync(User);

        // if (bruker is not Offentligaktør offentligAktor)
        //    return Forbid();

        var apneHendelser = HentApneHendelser();
        if (viewModel.CrisisId is int crisisId &&
            ((apneHendelser.Count > 0 && !apneHendelser.Contains(crisisId)) ||
             !await _context.Crises.AnyAsync(c => c.Id == crisisId)))
        {
            ModelState.AddModelError(
                nameof(viewModel.CrisisId),
                "Velg en gyldig krise.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Crises = await GetCrisesAsync(apneHendelser);
            return View(viewModel);
        }

        // Oppretter behovet med riktig aktørtilknytning.
        var need = new Need
        {
            Name = viewModel.Name.Trim(),
            Type = viewModel.Type.Trim(),
            Description = viewModel.Description.Trim(),
            Priority = viewModel.Priority,
            Status = viewModel.Status,
            Latitude = viewModel.Latitude!.Value,
            Longitude = viewModel.Longitude!.Value,
            CrisisId = viewModel.CrisisId,
            // OffentligAktorId = offentligAktor.Id
        };

        _context.Needs.Add(need);
        await _context.SaveChangesAsync();

        return LocalRedirect(this.OversiktUrl(viewModel.CrisisId));
    }

    [HttpGet]
    public IActionResult CreateMultiple() => RedirectToRoute("behov");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMultiple(
        MultipleNeedsViewModel viewModel)
    {
        if (viewModel.CrisisId is int crisisId &&
            !await _context.Crises.AnyAsync(c => c.Id == crisisId))
        {
            ModelState.AddModelError(nameof(viewModel.CrisisId), "Velg en gyldig krise.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Crises = await GetCrisesAsync();
            viewModel.ActiveCrisesOnMap = await GetActiveCrisesOnMapAsync();
            return View(viewModel);
        }

        var needs = viewModel.Needs.Select(item => new Need
        {
            Name = item.Name.Trim(),
            Type = item.Type.Trim(),
            Description = item.Description.Trim(),
            Priority = item.Priority,
            Status = item.Status,
            CrisisId = viewModel.CrisisId,
            Latitude = viewModel.Latitude!.Value,
            Longitude = viewModel.Longitude!.Value
        }).ToList();

        _context.Needs.AddRange(needs);
        await _context.SaveChangesAsync();

        TempData["NeedSuccessMessage"] = needs.Count == 1
            ? "Behovet er registrert."
            : $"{needs.Count} behov er registrert.";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                redirectUrl = this.OversiktUrl(viewModel.CrisisId)
            });
        }

        return LocalRedirect(this.OversiktUrl());
    }

    private async Task<IReadOnlyList<CrisisMapPoint>> GetActiveCrisesOnMapAsync()
    {
        return await _context.Crises
            .AsNoTracking()
            .Where(c => c.Status == "Aktiv"
                && c.Latitude >= -90 && c.Latitude <= 90
                && c.Longitude >= -180 && c.Longitude <= 180)
            .OrderBy(c => c.Name)
            .Select(c => new CrisisMapPoint(
                c.Id, c.Name, c.Latitude!.Value, c.Longitude!.Value))
            .ToListAsync();
    }

    // Henter krisene som kan velges i skjemaet.
    private List<int> HentApneHendelser() => Request.Query["hendelser"]
        .Select(value => int.TryParse(value, out var id) ? id : 0)
        .Where(id => id > 0)
        .Distinct()
        .ToList();

    private async Task<IEnumerable<SelectListItem>> GetCrisesAsync(List<int>? apneHendelser = null)
    {
        var crises = _context.Crises.AsQueryable();
        if (apneHendelser is { Count: > 0 })
            crises = crises.Where(c => apneHendelser.Contains(c.Id));

        return await crises
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var need = await _context.Needs
            .AsNoTracking()
            .SingleOrDefaultAsync(n => n.Id == id);

        if (need == null)
            return NotFound();

        ViewData["EditId"] = id;

        var viewModel = new NeedCreateViewModel
        {
            Name = need.Name,
            Type = need.Type,
            Description = need.Description,
            Priority = need.Priority,
            Status = need.Status,
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
        var need = await _context.Needs
            .SingleOrDefaultAsync(n => n.Id == id);

        if (need == null)
            return NotFound();

        if (viewModel.CrisisId is int crisisId &&
            !await _context.Crises.AnyAsync(c => c.Id == crisisId))
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
        need.Status = viewModel.Status;
        need.CrisisId = viewModel.CrisisId;
        need.Latitude = viewModel.Latitude;
        need.Longitude = viewModel.Longitude;

        await _context.SaveChangesAsync();

        return LocalRedirect(this.OversiktUrl());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var need = await _context.Needs
            .SingleOrDefaultAsync(n => n.Id == id);

        if (need == null)
            return NotFound();

        _context.Needs.Remove(need);
        await _context.SaveChangesAsync();

        return LocalRedirect(this.OversiktUrl());
    }
}
