using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Totalforsvaret.Controllers;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class MvcControllerNameAttribute(string name) : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller) => controller.ControllerName = name;
}
