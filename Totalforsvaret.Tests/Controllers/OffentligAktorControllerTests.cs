using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Controllers;
using Totalforsvaret.DataAccess;
using static Totalforsvaret.Tests.Helpers.PublicActorTestHelper;

namespace Totalforsvaret.Tests.Controllers;

/// <summary>
/// Unit tests for OffentligAktorController.
/// </summary>
public class OffentligAktorControllerTests
{
    /// <summary>
    /// Creates the controller with a signed-in user.
    /// </summary>
    private static OffentligAktorController CreateController(AppDbContext db, int? userId = 1)
    {
        // ResourceHandler is only used by Index, which is not tested here.
        return WithUser(new OffentligAktorController(null!, db), userId);
    }

    /// <summary>
    /// Only public actors have access to the controller.
    /// </summary>
    [Test]
    public void Controller_RequiresKunOffentligPolicy()
    {
        var attribute = typeof(OffentligAktorController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.That(attribute.Policy, Is.EqualTo("KunOffentlig"));
    }

    /// <summary>
    /// A requested resource is assigned when both need and resource are valid.
    /// </summary>
    [Test]
    public async Task TildelRessurs_ValidRequest_AssignsResource()
    {
        using var db = CreateDb();
        AddNeed(db, 1);
        AddResource(db, 1);
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Requested);

        var result = await CreateController(db).TildelRessurs(1);

        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        Assert.That(((LocalRedirectResult)result).Url, Is.EqualTo(OverviewUrl));
        Assert.That(db.ResourceAssignments.Single().Status, Is.EqualTo(AssignmentStatus.Assigned));
    }
    /// <summary>
    /// An assignment that does not exist returns NotFound.
    /// </summary>
    [Test]
    public async Task TildelRessurs_AssignmentNotFound_ReturnsNotFound()
    {
        using var db = CreateDb();

        var result = await CreateController(db).TildelRessurs(99);

        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
    }

    /// <summary>
    /// A resource that is already assigned cannot be assigned again.
    /// </summary>
    [Test]
    public async Task TildelRessurs_AlreadyAssigned_ReturnsBadRequest()
    {
        using var db = CreateDb();
        AddNeed(db, 1);
        AddResource(db, 1);
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Assigned);

        var result = await CreateController(db).TildelRessurs(1);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    /// <summary>
    /// A need that is resolved cannot receive new resources.
    /// </summary>
    [Test]
    public async Task TildelRessurs_NeedResolved_ReturnsBadRequest()
    {
        using var db = CreateDb();
        AddNeed(db, 1, status: "Løst");
        AddResource(db, 1);
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Requested);

        var result = await CreateController(db).TildelRessurs(1);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    /// <summary>
    /// A resource that is not available cannot be assigned.
    /// </summary>
    [Test]
    public async Task TildelRessurs_ResourceUnavailable_ReturnsBadRequest()
    {
        using var db = CreateDb();
        AddNeed(db, 1);
        AddResource(db, 1, available: false);
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Requested);

        var result = await CreateController(db).TildelRessurs(1);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }
    
    /// <summary>
    /// An assigned resource is released, and the assignment is kept as history.
    /// </summary>
    [Test]
    public async Task FrigiRessurs_Assigned_ReleasesResource()
    {
        using var db = CreateDb();
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Assigned);

        var result = await CreateController(db).FrigiRessurs(1);

        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        Assert.That(db.ResourceAssignments.Single().Status, Is.EqualTo(AssignmentStatus.Released));
    }

    /// <summary>
    /// A resource that is only requested cannot be released.
    /// </summary>
    [Test]
    public async Task FrigiRessurs_NotAssigned_ReturnsBadRequest()
    {
        using var db = CreateDb();
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Requested);

        var result = await CreateController(db).FrigiRessurs(1);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    /// <summary>
    /// A requested resource is rejected.
    /// </summary>
    [Test]
    public async Task AvslaRessurs_Requested_RejectsResource()
    {
        using var db = CreateDb();
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Requested);

        var result = await CreateController(db).AvslaRessurs(1);

        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        Assert.That(db.ResourceAssignments.Single().Status, Is.EqualTo(AssignmentStatus.Rejected));
    }

    /// <summary>
    /// A resource that is already assigned cannot be rejected.
    /// </summary>
    [Test]
    public async Task AvslaRessurs_AlreadyAssigned_ReturnsBadRequest()
    {
        using var db = CreateDb();
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Assigned);

        var result = await CreateController(db).AvslaRessurs(1);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }
}