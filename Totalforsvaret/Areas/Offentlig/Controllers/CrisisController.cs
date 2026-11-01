using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Controllers;

[Area("Offentlig")]
[Authorize(Policy = "KunOffentlig")]
public class CrisisController : Controller
{
    private readonly AppDbContext _context;

    // Gir controlleren tilgang til databasen.
    public CrisisController(AppDbContext context)
    {
        _context = context;
    }

    // Åpner kriseskjemaet.
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Crisis());
    }

    // Kontrollerer og lagrer krisen som sendes inn.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Area")] Crisis crisis)
    {
        // Fjerner mellomrom før og etter teksten.
        crisis.Name = crisis.Name?.Trim() ?? "";
        crisis.Description = crisis.Description?.Trim() ?? "";
        crisis.Area = crisis.Area?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(crisis.Name))
            ModelState.AddModelError(
                nameof(crisis.Name), "Skriv inn navnet på krisen.");
        else if (crisis.Name.Length > 100)
            ModelState.AddModelError(
                nameof(crisis.Name), "Navnet kan ha maksimalt 100 tegn.");

        if (string.IsNullOrWhiteSpace(crisis.Description))
            ModelState.AddModelError(
                nameof(crisis.Description), "Beskriv krisen.");
        else if (crisis.Description.Length > 1500)
            ModelState.AddModelError(
                nameof(crisis.Description),
                "Beskrivelsen kan ha maksimalt 1500 tegn.");

        if (string.IsNullOrWhiteSpace(crisis.Area))
            ModelState.AddModelError(
                nameof(crisis.Area), "Skriv inn området krisen gjelder.");
        else if (crisis.Area.Length > 100)
            ModelState.AddModelError(
                nameof(crisis.Area), "Området kan ha maksimalt 100 tegn.");

        // Viser skjemaet og feilmeldingene hvis noe er ugyldig.
        if (!ModelState.IsValid)
            return View(crisis);

        // Lagrer krisen i databasen.
        _context.Crises.Add(crisis);
        await _context.SaveChangesAsync();

        // Sender brukeren tilbake til den operative oversikten.
        return RedirectToAction(
            "Index", "OffentligAktor", new { area = "" });
    }
}