using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Totalforsvaret.Handlers;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;

namespace Totalforsvaret.Controllers;

public class HomeController : Controller
{
    private readonly ResourceHandler resourceHandler;

    public HomeController(ResourceHandler resourceHandler)
    {
        this.resourceHandler = resourceHandler;
    }

    private void PopulateCategories(FormDataModel model)
    {
        model.Categories = resourceHandler.GetCategories().Select(c =>
            new SelectListItem(c.Name, c.CategoryId.ToString(CultureInfo.InvariantCulture)));
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Map()
    {
        return View();
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

        var resource = resourceHandler.CreateResource(
            model.RessursType, model.CategoryId!.Value, model.Available!.Value,
            decimal.Parse(model.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture),
            decimal.Parse(model.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture),
            model.Navn, model.Kontaktpunkt, model.TilgjengeligFra);

        return View("Resultat", resource);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}