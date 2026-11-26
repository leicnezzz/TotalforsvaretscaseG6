using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Totalforsvaret.Models;

/// <summary>
/// Supplies resource categories to the registration wizard on public pages.
/// </summary>
public class PublicHomeViewModel
{
    public IReadOnlyList<SelectListItem> Categories { get; init; }
        = [];
}