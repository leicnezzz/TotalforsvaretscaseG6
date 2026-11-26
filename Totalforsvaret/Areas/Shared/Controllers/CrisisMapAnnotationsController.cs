using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;

namespace Totalforsvaret.Controllers;

/// <summary>
/// Handles map annotations attached to individual crises.
/// </summary>
[ApiController]
[Authorize(Policy = "KunOffentlig")]
[Route("api/crises/{crisisId:int}/map-annotations")]
public class CrisisMapAnnotationsController(
    ICrisisRepository crises, ICrisisMapAnnotationRepository annotations) : ControllerBase
{
    /// <summary>Carries the coordinates and descriptive fields used to register a crisis map annotation.</summary>
    public class AnnotationInput
    {
        [Required, MaxLength(20)] public string Kind { get; set; } = "";
        [Required, MaxLength(100)] public string Name { get; set; } = "";
        [MaxLength(1500)] public string Description { get; set; } = "";
        [MaxLength(100)] public string Impact { get; set; } = "";
        [MaxLength(10)] public string Risk { get; set; } = "";
        public double[][] Coordinates { get; set; } = [];
    }

    /// <summary>
    /// Adds a risk zone or road obstruction after checking its coordinates and shape.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int crisisId, AnnotationInput input)
    {
        if (crisisId <= 0 || !await crises.ExistsAsync(crisisId))
            return NotFound();
        if (input.Kind is not ("soner" or "veihindringer") ||
            string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100 ||
            input.Coordinates is null || input.Coordinates.Length > 500 ||
            input.Coordinates.Any(p => p is null || p.Length != 2 ||
                !double.IsFinite(p[0]) || !double.IsFinite(p[1]) ||
                Math.Abs(p[0]) > 90 || Math.Abs(p[1]) > 180))
            return BadRequest("Ugyldig kartmarkering.");
        if (input.Kind == "soner")
        {
            if (input.Risk is not ("lav" or "moderat" or "hoy") ||
                input.Coordinates.Select(p => (p[0], p[1])).Distinct().Count() < 3)
                return BadRequest("Velg risiko og minst tre forskjellige hjørner.");
            double area = 0;
            for (int i = 0; i < input.Coordinates.Length; i++)
            {
                var a = input.Coordinates[i];
                var b = input.Coordinates[(i + 1) % input.Coordinates.Length];
                area += a[0] * b[1] - b[0] * a[1];
            }
            if (Math.Abs(area) < 1e-12) return BadRequest("Sonen må ha et areal.");
        }
        else if (input.Coordinates.Length != 1)
            return BadRequest("En veihindring må ha ett punkt.");
        var annotation = new CrisisMapAnnotation
        {
            CrisisId = crisisId, Kind = input.Kind, Name = input.Name.Trim(),
            Description = input.Description.Trim(), Impact = input.Impact.Trim(),
            Risk = input.Kind == "soner" ? input.Risk : "",
            CoordinatesJson = JsonSerializer.Serialize(input.Coordinates)
        };
        await annotations.AddAsync(annotation);
        return Ok(new { id = annotation.Id, crisisId });
    }

    /// <summary>
    /// Removes an annotation only when it belongs to the specified crisis.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int crisisId, int id)
    {
        if (!await annotations.DeleteAsync(crisisId, id))
            return NotFound();
        return NoContent();
    }
}
