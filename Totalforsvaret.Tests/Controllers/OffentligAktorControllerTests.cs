using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Controllers;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.resourcemodels;
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
    
    /// <summary>
    /// The user only sees their own resources.
    /// </summary>
    [Test]
    public async Task MineRessurser_ShowsOnlyOwnResources()
    {
        using var db = CreateDb();
        AddResource(db, 1, userId: 1);
        AddResource(db, 2, userId: 2);

        var result = await CreateController(db, userId: 1).MineRessurser();

        Assert.That(result, Is.TypeOf<ViewResult>());
        var resources = (IEnumerable<Resource>)((ViewResult)result).Model!;
        Assert.That(resources.Select(r => r.ResourceId), Is.EqualTo(new[] { 1 }));
    }

    /// <summary>
    /// Without a signed-in user, the user is asked to sign in.
    /// </summary>
    [Test]
    public async Task MineRessurser_NotSignedIn_ReturnsChallenge()
    {
        using var db = CreateDb();

        var result = await CreateController(db, userId: null).MineRessurser();

        Assert.That(result, Is.TypeOf<ChallengeResult>());
    }
    
    /// <summary>
    /// A new resource is saved on the signed-in user.
    /// </summary>
    [Test]
    public async Task NyRessurs_Valid_SavesOnSignedInUser()
    {
        using var db = CreateDb();
        AddResource(db, 1); // makes sure category 1 exists

        var result = await CreateController(db, userId: 7)
            .NyRessurs("Truck", 1, null, "Duty officer", "Street 1", 59.9m, 10.7m, null, null);

        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        var created = db.Resources.Single(r => r.Object == "Truck");
        Assert.That(created.BrukerId, Is.EqualTo(7));
    }

    /// <summary>
    /// A resource with a category that does not exist is not saved.
    /// </summary>
    [Test]
    public async Task NyRessurs_UnknownCategory_IsNotSaved()
    {
        using var db = CreateDb();

        var result = await CreateController(db)
            .NyRessurs("Truck", 42, null, "Duty officer", "Street 1", 59.9m, 10.7m, null, null);

        Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        Assert.That(db.Resources, Is.Empty);
    }
}