using Microsoft.AspNetCore.Mvc;

namespace Totalforsvaret.Controllers;

public class NeedController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
}