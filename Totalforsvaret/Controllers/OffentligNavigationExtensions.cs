using Microsoft.AspNetCore.Mvc;

namespace Totalforsvaret.Controllers;

internal static class OffentligNavigationExtensions
{
    public static string OversiktUrl(this Controller controller, int? fallbackCrisisId = null, int? openCrisisId = null)
    {
        var url = controller.Url.Action("Index", "OffentligAktor", new { area = "" })!;
        var ids = controller.Request.Query["hendelser"]
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (ids.Count == 0 && fallbackCrisisId is int id)
            ids.Add(id);
        if (openCrisisId is int openId && !ids.Contains(openId))
            ids.Add(openId);

        return url + QueryString.Create(ids.Select(id =>
            new KeyValuePair<string, string?>("hendelser", id.ToString())));
    }
}
