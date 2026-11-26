using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Totalforsvaret.Controllers;

/// <summary>
/// Keeps the MVC routing name stable when a controller class is renamed.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class MvcControllerNameAttribute : Attribute, IControllerModelConvention {
    private readonly string _name;

    public MvcControllerNameAttribute(string name)
    {
        _name = name;
    }

    public void Apply(ControllerModel controller) => controller.ControllerName = _name;
}
