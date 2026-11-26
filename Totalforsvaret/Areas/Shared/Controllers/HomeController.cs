using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Totalforsvaret.DataAccess;
namespace Totalforsvaret.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index([FromServices] IResourceRepository resourceHandler)
    {
        return View(BuildResourceModel(resourceHandler));
    }

    /// <summary>Opens the public resource registration wizard with its available categories.</summary>
    [Authorize(Policy = "KunOffentlig")]
    [HttpGet("/OffentligAktor/RegistrerRessurs", Name = "ressursregistrering")]
    [HttpGet("/Ressurs")]
    public IActionResult ResourceRegistration([FromServices] IResourceRepository resourceHandler)
    {
        return View(BuildResourceModel(resourceHandler));
    }

    private static PublicHomeViewModel BuildResourceModel(IResourceRepository resourceHandler)
    {
        return new PublicHomeViewModel
        {
            Categories = resourceHandler.GetCategories()
                .Select(category => new SelectListItem(category.Name,
                    category.CategoryId.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                .ToList()
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

	[AllowAnonymous]
	[HttpGet]
	public IActionResult Contact()
	{
		return View();
	}
    /// <summary>Shows the request identifier for correlating an error page with server logs.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
