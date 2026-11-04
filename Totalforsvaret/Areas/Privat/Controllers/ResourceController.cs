using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Handlers;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResourceController(
    AppDbContext db,
    ResourceHandler resourceHandler,
    UserManager<Bruker> users) : Controller
{
    [HttpGet]
    public IActionResult Skjema()
    {
        var model = new FormDataModel
        {
            Categories = resourceHandler.GetCategories()
                .Select(category => new SelectListItem(
                    category.Name,
                    category.CategoryId.ToString(
                        CultureInfo.InvariantCulture)))
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Oversikt()
    {
        var bruker = await users.GetUserAsync(User);

        if (bruker is null)
            return Challenge();

        var resources = await db.Resources
            .AsNoTracking()
            .Include(resource => resource.Category)
            .Include(resource => resource.Vehicle)
            .Include(resource => resource.Equipment)
            .Where(resource => resource.BrukerId == bruker.Id)
            .OrderByDescending(resource => resource.ResourceId)
            .ToListAsync();

        return View(resources);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromBody] ResourceInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var bruker = await users.GetUserAsync(User);

        if (bruker is null)
            return Challenge();

        var categoryExists = await db.Categories
            .AnyAsync(category => category.CategoryId == input.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "Kategorien finnes ikke." });

        var resource = new Resource
        {
            Object = input.Object.Trim(),
            BrukerId = bruker.Id
        };

        ApplyInput(resource, input);

        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        return Ok(new { resourceId = resource.ResourceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ResourceInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var bruker = await users.GetUserAsync(User);

        if (bruker is null)
            return Challenge();

        var resource = await db.Resources
            .Include(resource => resource.Vehicle)
            .Include(resource => resource.Equipment)
            .SingleOrDefaultAsync(resource =>
                resource.ResourceId == id &&
                resource.BrukerId == bruker.Id);

        if (resource is null)
            return NotFound();

        // Redigering beholder ressursens opprinnelige kategori.
        if (resource.CategoryId != input.CategoryId)
        {
            return BadRequest(new
            {
                message = "Ressurstypen kan ikke endres her."
            });
        }

        ApplyInput(resource, input);
        await db.SaveChangesAsync();

        return Ok(new { resourceId = resource.ResourceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var bruker = await users.GetUserAsync(User);

        if (bruker is null)
            return Challenge();

        var resource = await db.Resources
            .SingleOrDefaultAsync(resource =>
                resource.ResourceId == id &&
                resource.BrukerId == bruker.Id);

        if (resource is null)
            return NotFound();

        db.Resources.Remove(resource);
        await db.SaveChangesAsync();

        return Ok(new { resourceId = id });
    }

    private static void ApplyInput(
        Resource resource,
        ResourceInput input)
    {
        resource.Object = input.Object.Trim();
        resource.CategoryId = input.CategoryId!.Value;
        resource.Available = input.Available!.Value;
        resource.Kontaktpunkt = input.Kontaktpunkt.Trim();
        resource.TilgjengeligFra = input.TilgjengeligFra;
        resource.Address = input.Address.Trim();
        resource.Latitude = input.Latitude!.Value;
        resource.Longitude = input.Longitude!.Value;
        resource.Condition = input.Condition.Trim();
        resource.AdditionalInformation =
            input.AdditionalInformation?.Trim() ?? "";

        if (resource.CategoryId == 1)
        {
            resource.Vehicle ??= new VehicleDetails();

            resource.Vehicle.RegistrationNumber =
                string.IsNullOrWhiteSpace(input.Vehicle!.RegistrationNumber)
                    ? null
                    : input.Vehicle.RegistrationNumber.Trim().ToUpperInvariant();

            resource.Vehicle.RequiredLicense =
                input.Vehicle.RequiredLicense?.Trim() ?? "";

            resource.Equipment = null;
        }
        else
        {
            resource.Equipment ??= new EquipmentDetails();

            resource.Equipment.EquipmentType =
                input.Equipment!.EquipmentType;

            resource.Equipment.Quantity =
                input.Equipment.Quantity!.Value;

            resource.Vehicle = null;
        }
    }
}
