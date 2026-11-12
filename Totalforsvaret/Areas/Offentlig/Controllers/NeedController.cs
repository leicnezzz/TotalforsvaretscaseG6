
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
    public async Task<IActionResult> Create()
    {
        var viewModel = new NeedCreateViewModel
        {
            Crises = await GetCrisesAsync()
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

        if (viewModel.CrisisId is int crisisId &&
            !await _context.Crises.AnyAsync(c => c.Id == crisisId))
        {
            ModelState.AddModelError(
                nameof(viewModel.CrisisId),
                "Velg en gyldig krise.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Crises = await GetCrisesAsync();
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

        return RedirectToAction(
            "Index", "OffentligAktor", new { area = "" });
    }
    [HttpGet]
    public async Task<IActionResult> CreateMultiple()
    {
        return View(new MultipleNeedsViewModel
        {
            Crises = await GetCrisesAsync()
        });
    }

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
                redirectUrl = Url.Action("Index", "OffentligAktor", new { area = "" })
            });
        }

        return RedirectToAction(
            "Index", "OffentligAktor", new { area = "" });
    }
    // Henter krisene som kan velges i skjemaet.
    private async Task<IEnumerable<SelectListItem>> GetCrisesAsync()
    {
        return await _context.Crises
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

    return RedirectToAction(
        "Index", "OffentligAktor", new { area = "" });
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

    return RedirectToAction(
        "Index",
        "OffentligAktor",
        new { area = "" });
}
}
