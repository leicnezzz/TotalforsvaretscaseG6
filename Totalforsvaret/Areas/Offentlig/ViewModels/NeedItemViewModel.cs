using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

public class NeedItemViewModel
{
    [Required(ErrorMessage = "Skriv inn navnet på behovet.")]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Velg type behov.")]
    [RegularExpression("^(Transport|Utstyr)$",
        ErrorMessage = "Velg kjøretøy eller utstyr.")]
    public string Type { get; set; } = "";

    [Required(ErrorMessage = "Beskriv behovet.")]
    [StringLength(1500)]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Velg prioritet.")]
    public string Priority { get; set; } = "";

    [Required(ErrorMessage = "Velg status.")]
    public string Status { get; set; } = "Åpen";
}