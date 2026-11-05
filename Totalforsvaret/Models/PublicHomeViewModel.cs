using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Totalforsvaret.Models;
// Informasjon som vises på forsiden. Inneholder kategorier som vises i menyen. 
public class PublicHomeViewModel
{
    public IReadOnlyList<SelectListItem> Categories { get; init; }
        = Array.Empty<SelectListItem>();
}