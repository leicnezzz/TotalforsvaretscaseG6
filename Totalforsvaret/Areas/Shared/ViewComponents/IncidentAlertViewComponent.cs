using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.DataAccess;

namespace Totalforsvaret.Areas.Shared.ViewComponents;

public class IncidentAlertViewComponent(ICrisisRepository crises) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var crisis = await crises.FindLatestAlertAsync(HttpContext.RequestAborted);

        return crisis is null
            ? Content(string.Empty)
            : View("~/Areas/Shared/Views/Shared/_IncidentAlert.cshtml", crisis);
    }
}
