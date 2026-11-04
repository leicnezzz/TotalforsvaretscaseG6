using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]

public class NeedController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
}