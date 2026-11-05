using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public class NeedCreateViewModel
{
    [Required(ErrorMessage = "Skriv inn navnet på behovet.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Velg type behov.")]
    [RegularExpression("^(Transport|Utstyr)$",
        ErrorMessage = "Velg kjøretøy eller utstyr.")]
    public string Type { get; set; } = "";

    [Required(ErrorMessage = "Beskriv behovet.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Velg prioritet.")]
    public string Priority { get; set; } = "";

    [Required(ErrorMessage = "Velg status.")]
    public string Status { get; set; } = "";

    [Required(ErrorMessage = "Velg en krise.")]
    public int CrisisId { get; set; }

    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    public double? Longitude { get; set; }

    public IEnumerable<SelectListItem> Crises { get; set; }
        = new List<SelectListItem>();
}