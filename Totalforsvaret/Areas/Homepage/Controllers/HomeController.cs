using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Totalforsvaret.Handlers;
namespace Totalforsvaret.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index([FromServices] ResourceHandler resourceHandler)
    {
        var model = new PublicHomeViewModel
        {
            Categories = resourceHandler.GetCategories()
                .Select(category => new SelectListItem(category.Name,
                    category.CategoryId.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                .ToList()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Map()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
