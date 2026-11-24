using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public class NeedCreateViewModel
{
    [Required(ErrorMessage = "Skriv inn navnet på behovet.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Velg type behov.")]
    [AllowedValues(NeedType.Transport, NeedType.Equipment, ErrorMessage = "Velg kjøretøy eller utstyr.")]
    public NeedType? Type { get; set; }

    [Required(ErrorMessage = "Beskriv behovet.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Velg prioritet.")]
    [AllowedValues(NeedPriority.Low, NeedPriority.Medium, NeedPriority.High, NeedPriority.Critical,
        ErrorMessage = "Velg en gyldig prioritet.")]
    public NeedPriority? Priority { get; set; }

    [Required(ErrorMessage = "Velg status.")]
    [AllowedValues(NeedStatus.Open, NeedStatus.InProgress, NeedStatus.Resolved, ErrorMessage = "Velg en gyldig status.")]
    public NeedStatus? Status { get; set; }

    [Required(ErrorMessage = "Velg en krise.")]
    public int? CrisisId { get; set; }

    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    [Range(typeof(decimal), "-90", "90", ErrorMessage = "Ugyldig breddegrad.")]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    [Range(typeof(decimal), "-180", "180", ErrorMessage = "Ugyldig lengdegrad.")]
    public decimal? Longitude { get; set; }

    public IReadOnlyList<CrisisMapPoint> CrisisLocations { get; set; } = [];

    public IEnumerable<SelectListItem> Crises { get; set; }
        = new List<SelectListItem>();
}