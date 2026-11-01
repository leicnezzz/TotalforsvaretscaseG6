using Microsoft.AspNetCore.Mvc.Rendering;

namespace Totalforsvaret.Models;

public class PublicHomeViewModel
{
    public IReadOnlyList<SelectListItem> Categories { get; init; }
        = Array.Empty<SelectListItem>();
}