using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Totalforsvaret.Controllers;

/// <summary>
/// Keeps the MVC routing name stable when a controller class is renamed.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class MvcControllerNameAttribute(string name) : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller) => controller.ControllerName = name;
}
