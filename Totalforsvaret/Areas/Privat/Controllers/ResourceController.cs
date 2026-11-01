using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Totalforsvaret.Handlers;
using Totalforsvaret.Areas.Privat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Totalforsvaret.Models;


namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
public class ResourceController(ResourceHandler resourceHandler, UserManager<Bruker> users) : Controller
{
    private void PopulateCategories(FormDataModel model)
    {
        model.Categories = resourceHandler.GetCategories().Select(c =>
            new SelectListItem(c.Name, c.CategoryId.ToString(CultureInfo.InvariantCulture)));
    }

    [HttpGet]
    public IActionResult Skjema()
    {
        var model = new FormDataModel();
        PopulateCategories(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Skjema(FormDataModel model)
    {
        if (!resourceHandler.GetCategories().Any(c => c.CategoryId == model.CategoryId))
            ModelState.AddModelError(nameof(model.CategoryId), "Velg en gyldig kategori.");

        if (!ModelState.IsValid)
        {
            PopulateCategories(model);
            return View(model);
        }

        if (!int.TryParse(users.GetUserId(User), out var brukerId))
        {
            return Challenge();
        }

        var resource = resourceHandler.CreateResource(
            model.RessursType,
            model.CategoryId!.Value,
            model.Available!.Value,
            decimal.Parse(
              model.Latitude,
              NumberStyles.Float,
              CultureInfo.InvariantCulture),
            decimal.Parse(
              model.Longitude,
              NumberStyles.Float,
            CultureInfo.InvariantCulture),
            brukerId,
            model.Navn,
            model.Kontaktpunkt,
            model.TilgjengeligFra);

        return View("Resultat", resource);
    }

    [HttpGet]
    public IActionResult Oversikt()
    {
        return View();
    }
}
