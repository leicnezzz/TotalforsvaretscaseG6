using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.Models;

namespace Totalforsvaret.Controllers;

public class HomeController : Controller
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Index()
    {
        if (User.HasClaim("Brukertype", "Privat"))
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Privat" });
        }

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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
