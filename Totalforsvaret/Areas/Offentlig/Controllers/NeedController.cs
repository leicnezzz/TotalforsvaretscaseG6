using Microsoft.AspNetCore.Mvc;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
public class NeedController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
}