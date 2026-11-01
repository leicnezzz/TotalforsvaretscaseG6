using Microsoft.AspNetCore.Mvc;
// Gir tilgant til gruppens eksisterende ressushåndteirng 
using Totalforsvaret.Handlers;

namespace Totalforsvaret.Controllers;

public class OffentligAktorController : Controller
{
    // Gir Controlleren tilgang til ressursene 
    private readonly ResourceHandler _resourceHandler;

    //Motter ResourceHandler fra Program.cs
    public OffentligAktorController(ResourceHandler resourceHandler)
    {
        _resourceHandler = resourceHandler;
    }

    public IActionResult Index()
    {
        // Henter alle registrerte ressurser
        var ressurser = _resourceHandler.GetResources();

        // Velger bare ressursene som er tilgjengelige
        var tilgjengeligeRessurser = ressurser
            .Where(r => r.Available)
            .ToList();

        // Sender ressursene videre til nettsiden
        return View(tilgjengeligeRessurser);
    }
}