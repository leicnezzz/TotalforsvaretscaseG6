using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Totalforsvaret.Areas.Privat.Controllers;

[Area("Privat")]
[Authorize(Policy = "KunPrivat")]
public class EgenberedskapController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}