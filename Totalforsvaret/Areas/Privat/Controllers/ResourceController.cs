using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResourceController(
    IResourceRepository resources,
    UserManager<Bruker> users) : Controller
{
    [Authorize(Policy = "KunPrivat")]
    [HttpGet]
    [ActionName("Skjema")]
    public IActionResult Registration()
    {
        var model = new FormDataModel
        {
            Categories = resources.GetCategories()
                .Select(category => new SelectListItem(
                    category.Name,
                    category.CategoryId.ToString(
                        CultureInfo.InvariantCulture)))
                .ToList()
        };

        return View(model);
    }

    [Authorize(Policy = "KunPrivat")]
    [HttpGet]
    [ActionName("Oversikt")]
    public async Task<IActionResult> Overview()
    {
        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var ownedResources = await resources.ListOwnedAsync(user.Id);

        return View(ownedResources);
    }

    [Authorize(Policy = "KunPrivat")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(
        [FromBody] ResourceInput input)
    {
        return CreateResource(input);
    }

    [Authorize(Policy = "KunOffentlig")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateForPublicActor([FromBody] PublicResourceInput input)
    {
        return CreateResource(input, input);
    }

    private async Task<IActionResult> CreateResource(ResourceInput input, PublicResourceInput? contact = null)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var categoryExists = await resources.CategoryExistsAsync(input.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "Kategorien finnes ikke." });

        var resource = new Resource
        {
            Object = input.Object.Trim(),
            BrukerId = user.Id
        };

        ApplyInput(resource, input);
        if (contact is not null)
        {
            resource.Navn = $"{contact.ContactFirstName.Trim()} {contact.ContactLastName.Trim()}";
            resource.Kontaktpunkt = $"{resource.Kontaktpunkt} · {contact.ContactEmail.Trim()}";
        }


        await resources.AddAsync(resource);

        return Ok(new { resourceId = resource.ResourceId });
    }

    [Authorize(Policy = "KunPrivat")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ResourceInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();
		// Søker både ressurs-ID og user-ID.
		// Gjør at brukeren bare redigere sine egne ressurser.
		var resource = await resources.FindOwnedAsync(id, user.Id, includeDetails: true);

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
        await resources.SaveChangesAsync();

        return Ok(new { resourceId = resource.ResourceId });
    }

    [Authorize(Policy = "KunPrivat")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var resource = await resources.FindOwnedAsync(id, user.Id);

        if (resource is null)
            return NotFound();

        if (!await resources.TryDeleteAsync(resource))
        {
            return Conflict(new { message = "Ressursen har vært brukt i en tildeling og kan ikke slettes." });
        }

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
