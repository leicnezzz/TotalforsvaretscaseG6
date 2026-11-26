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

/// <summary>
/// Handles resource registration and owner-managed changes.
/// </summary>
[Area("Privat")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResourceController : Controller {
    private readonly IResourceRepository _resources;
    private readonly UserManager<Bruker> _users;

    public ResourceController(IResourceRepository resources, UserManager<Bruker> users)
    {
        _resources = resources;
        _users = users;
    }

    [Authorize(Policy = "KunPrivat")]
    [HttpGet]
    [ActionName("Skjema")]
    public IActionResult Registration()
    {
        var model = new FormDataModel
        {
            Categories = _resources.GetCategories()
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
        var user = await _users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var ownedResources = await _resources.ListOwnedAsync(user.Id);

        return View(ownedResources);
    }

    [Authorize(Policy = "KunPrivat")]
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(
        [FromBody] ResourceInput input)
    {
        return CreateResource(input);
    }

    /// <summary>
    /// Registers a resource for the signed-in public actor and records the supplied contact
    /// details.
    /// </summary>
    [Authorize(Policy = "KunOffentlig")]
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> CreateForPublicActor([FromBody] PublicResourceInput input)
    {
        return CreateResource(input, input);
    }

    private async Task<IActionResult> CreateResource(ResourceInput input, PublicResourceInput? contact = null)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var categoryExists = await _resources.CategoryExistsAsync(input.CategoryId);

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


        await _resources.AddAsync(resource);

        return Ok(new { resourceId = resource.ResourceId });
    }

    /// <summary>
    /// Updates an owned resource while preserving its original category.
    /// </summary>
    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ResourceInput input)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _users.GetUserAsync(User);

        if (user is null)
            return Challenge();
		var resource = await _resources.FindOwnedAsync(id, user.Id, includeDetails: true);

        if (resource is null)
            return NotFound();

        if (resource.CategoryId != input.CategoryId)
        {
            return BadRequest(new
            {
                message = "Ressurstypen kan ikke endres her."
            });
        }

        var email = user is Totalforsvaret.Areas.Offentlig.Models.PublicActor
            ? resource.Kontaktpunkt.Split(" · ", 2).ElementAtOrDefault(1)
            : null;
        ApplyInput(resource, input);
        if (!string.IsNullOrEmpty(email))
            resource.Kontaktpunkt = $"{resource.Kontaktpunkt} · {email}";

        await _resources.SaveChangesAsync();

        return Ok(new { resourceId = resource.ResourceId });
    }

    /// <summary>
    /// Deletes an owned resource if it has no assignment history.
    /// </summary>
    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _users.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var resource = await _resources.FindOwnedAsync(id, user.Id);

        if (resource is null)
            return NotFound();

        if (!await _resources.TryDeleteAsync(resource))
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
