using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.DataAccess;

namespace Totalforsvaret.Areas.Shared.ViewComponents;

public class IncidentAlertViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var crisis = await db.Crises
            .AsNoTracking()
            .Where(crisis => (crisis.Status == "Aktiv" || crisis.Status == "Under kontroll") &&
                (crisis.Severity == "Kritisk" || crisis.Severity == "Høy"))
            .OrderByDescending(crisis => crisis.Id)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);

        return crisis is null
            ? Content(string.Empty)
            : View("~/Areas/Shared/Views/Shared/_IncidentAlert.cshtml", crisis);
    }
}
