using Microsoft.AspNetCore.Mvc;
using Totalforsvaret.DataAccess;

namespace Totalforsvaret.Areas.Homepage.ViewComponents;

public class IncidentAlertViewComponent(ICrisisRepository crises) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var crisis = await crises.FindLatestAlertAsync(HttpContext.RequestAborted);

        return crisis is null
            ? Content(string.Empty)
            : View("~/Areas/Homepage/Views/Shared/_IncidentAlert.cshtml", crisis);
    }
}
