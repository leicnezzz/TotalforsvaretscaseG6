using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Tests.Helpers;

/// <summary>
/// Shared setup for the public actor tests.
/// </summary>
public static class PublicActorTestHelper
{
    /// <summary>
    /// The URL the controller redirects to after a successful action.
    /// </summary>
    public const string OverviewUrl = "/OffentligAktor/Index";

    /// <summary>
    /// Creates a new, empty in-memory database.
    /// </summary>
    public static AppDbContext CreateDb()
    {
        // A unique name keeps tests from sharing data with each other.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Gives the controller a signed-in user, TempData and a URL helper.
    /// Pass userId = null to test without a signed-in user.
    /// </summary>
    public static T WithUser<T>(T controller, int? userId = 1) where T : Controller
    {
        var claims = new List<Claim>();
        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()!));

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // The controller builds redirect URLs with Url.Action.
        // Returning "/Controller/Action" lets the tests check the URL.
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns((UrlActionContext c) => $"/{c.Controller ?? "OffentligAktor"}/{c.Action}");
        controller.Url = url.Object;

        return controller;
    }

    /// <summary>
    /// Adds a need to the database.
    /// </summary>
    public static Need AddNeed(AppDbContext db, int id, NeedStatus status = NeedStatus.Open)
    {
        var need = new Need { Id = id, Name = $"Need {id}", Type = "Transport", Status = status };
        db.Needs.Add(need);
        db.SaveChanges();
        return need;
    }

    /// <summary>
    /// Adds a resource, and the category it requires, to the database.
    /// </summary>
    public static Resource AddResource(
        AppDbContext db, int id, int userId = 1, bool available = true)
    {
        if (!db.Categories.Any(c => c.CategoryId == 1))
            db.Categories.Add(new Category { CategoryId = 1, Name = "Vehicles" });

        var resource = new Resource
        {
            ResourceId = id,
            Object = $"Resource {id}",
            BrukerId = userId,
            CategoryId = 1,
            Available = available,
            Navn = "Test",
            Kontaktpunkt = "Duty officer",
            Address = "Test Street 1",
            Condition = "",
            AdditionalInformation = ""
        };
        db.Resources.Add(resource);
        db.SaveChanges();
        return resource;
    }

    /// <summary>
    /// Adds an assignment between a need and a resource.
    /// </summary>
    public static ResourceAssignment AddAssignment(
        AppDbContext db, int id, int needId, int resourceId, AssignmentStatus status)
    {
        var assignment = new ResourceAssignment
        {
            Id = id,
            NeedId = needId,
            ResourceId = resourceId,
            Status = status,
            RequestedAtUtc = DateTime.UtcNow
        };
        db.ResourceAssignments.Add(assignment);
        db.SaveChanges();
        return assignment;
    }
}