using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public class MultipleNeedsViewModel
{
    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    [Range(-90d, 90d, ErrorMessage = "Ugyldig breddegrad.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Velg en lokasjon på kartet.")]
    [Range(-180d, 180d, ErrorMessage = "Ugyldig lengdegrad.")]
    public double? Longitude { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Legg til minst ett behov.")]
    public List<NeedItemViewModel> Needs { get; set; }
        = new() { new NeedItemViewModel() };
}
