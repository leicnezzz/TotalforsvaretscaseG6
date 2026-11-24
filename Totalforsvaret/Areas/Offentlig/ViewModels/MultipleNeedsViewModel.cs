using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public record CrisisMapPoint(int Id, string Name, decimal Latitude, decimal Longitude);

public class MultipleNeedsViewModel
{
    [Required(ErrorMessage = "Velg en krise.")]
    public int? CrisisId { get; set; }

    public IEnumerable<SelectListItem> Crises { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<CrisisMapPoint> ActiveCrisesOnMap { get; set; } = Array.Empty<CrisisMapPoint>();

    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    [Range(-90d, 90d, ErrorMessage = "Ugyldig breddegrad.")]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    [Range(-180d, 180d, ErrorMessage = "Ugyldig lengdegrad.")]
    public decimal? Longitude { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Legg til minst ett behov.")]
    public List<NeedItemViewModel> Needs { get; set; }
        = new() { new NeedItemViewModel() };
}
