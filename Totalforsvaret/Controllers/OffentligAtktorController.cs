using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Handlers;
using Totalforsvaret.DataAccess;

namespace Totalforsvaret.Controllers;

public class OffentligAktorController : Controller
{
    private readonly ResourceHandler _resourceHandler;
    private readonly AppDbContext? _context;

    public OffentligAktorController(
        ResourceHandler resourceHandler,
        AppDbContext? context = null)
    {
        _resourceHandler = resourceHandler;
        _context = context;
    }

    public IActionResult Index()
    {
        // Henter tilgjengelige ressurser
        var ressurser = _resourceHandler.GetResources();

        var tilgjengeligeRessurser = ressurser
            .Where(r => r.Available)
            .ToList();

        // Henter antall aktive behov
        ViewBag.AktiveBehov = _context?.Needs.Count(n =>
            n.Status == "Åpen" ||
            n.Status == "Under behandling") ?? 0;
        
        // Henter antall løste behov.
        ViewBag.LosteBehov = _context?.Needs.Count(n =>
            n.Status == "Løst") ?? 0;

        // Henter aktive behov med gyldige kartkoordinater
        ViewBag.BehovPaKart = _context?.Needs
            .Where(n =>
                (n.Status == "Åpen" || n.Status == "Under behandling") &&
                n.Latitude != null &&
                n.Longitude != null)
            .ToList();
        
        // Henter de fem nyeste aktive behovene
        ViewBag.AktiveBehovListe = _context?.Needs
            .Where(n => n.Status == "Åpen" || n.Status == "Under behandling")
            .OrderByDescending(n => n.Id)
            .Take(5)
            .ToList();
        
        // Henter aktive kriser med kartkoordinater.
        ViewBag.KriserPaKart = _context?.Crises
            .AsNoTracking()
            .Where(c => c.Status == "Aktiv" &&
                        c.Latitude != null &&
                        c.Longitude != null)
            .Select(c => new
            {
                navn = c.Name,
                type = c.Type,
                alvorlighetsgrad = c.Severity,
                status = c.Status,
                beskrivelse = c.Description,
                latitude = c.Latitude,
                longitude = c.Longitude
            })
            .ToList();
        
        // Alle aktive og løste behov til kortenes lister.
        ViewBag.BehovTilLister = _context?.Needs
            .AsNoTracking()
            .Where(n => n.Status == "Åpen" ||
                        n.Status == "Under behandling" ||
                        n.Status == "Løst")
            .Select(n => new
            {
                id = n.Id,
                navn = n.Name,
                type = n.Type,
                prioritet = n.Priority,
                status = n.Status,
                krise = n.Crisis != null ? n.Crisis.Name : "",
                latitude = n.Latitude,
                longitude = n.Longitude
            })
            .ToList();
        return View(tilgjengeligeRessurser);
    }
}