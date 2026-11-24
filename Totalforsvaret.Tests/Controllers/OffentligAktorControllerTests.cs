using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Moq;
using Totalforsvaret.Areas.Privat.Controllers;
using Totalforsvaret.Models;
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
    private static ResourceController CreateResourceController(AppDbContext db, int userId)
    {
        var users = new Mock<UserManager<Bruker>>(
            Mock.Of<IUserStore<Bruker>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(new Offentligaktør { Id = userId });
        return WithUser(new ResourceController(new ResourceRepository(db), users.Object), userId);
    }

    private static PublicResourceInput CreateResourceInput(int categoryId) => new()
    {
        Object = "Truck",
        CategoryId = categoryId,
        Available = true,
        Condition = "Good",
        Vehicle = new VehicleInput(),
        Kontaktpunkt = "+4712345678",
        Address = "Street 1",
        Latitude = 59.9m,
        Longitude = 10.7m,
        ContactFirstName = "Test",
        ContactLastName = "User",
        ContactEmail = "test@example.com"
    };

    /// <summary>
    /// Creates the controller with a signed-in user.
    /// </summary>
    private static OffentligAktorController CreateController(AppDbContext db, int? userId = 1)
    {
        // Use the same repositories as the application.
        return WithUser(new OffentligAktorController(new ResourceRepository(db), new ResourceAssignmentRepository(db), new PublicOverviewRepository(db)), userId);
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
        AddNeed(db, 1, status: NeedStatus.Resolved);
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
    public async Task CreateForPublicActor_Valid_SavesOnSignedInUser()
    {
        using var db = CreateDb();
        AddResource(db, 1); // makes sure category 1 exists

        var result = await CreateResourceController(db, 7).CreateForPublicActor(CreateResourceInput(1));

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var created = db.Resources.Single(r => r.Object == "Truck");
        Assert.That(created.BrukerId, Is.EqualTo(7));
    }

    /// <summary>
    /// A resource with a category that does not exist is not saved.
    /// </summary>
    [Test]
    public async Task CreateForPublicActor_UnknownCategory_IsNotSaved()
    {
        using var db = CreateDb();

        var result = await CreateResourceController(db, 1).CreateForPublicActor(CreateResourceInput(42));

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That(db.Resources, Is.Empty);
    }

    /// <summary>
    /// The user's own resource without assignments is deleted.
    /// </summary>
    [Test]
    public async Task SlettRessurs_OwnResource_IsDeleted()
    {
        using var db = CreateDb();
        AddResource(db, 1, userId: 1);

        await CreateController(db, userId: 1).SlettRessurs(1);

        Assert.That(db.Resources, Is.Empty);
    }

    /// <summary>
    /// A user cannot delete another user's resource.
    /// </summary>
    [Test]
    public async Task SlettRessurs_OtherUsersResource_ReturnsNotFound()
    {
        using var db = CreateDb();
        AddResource(db, 1, userId: 2);

        var result = await CreateController(db, userId: 1).SlettRessurs(1);

        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        Assert.That(db.Resources.Count(), Is.EqualTo(1));
    }

    /// <summary>
    /// A resource that has been assigned is kept for history.
    /// </summary>
    [Test]
    public async Task SlettRessurs_HasBeenAssigned_IsNotDeleted()
    {
        using var db = CreateDb();
        AddResource(db, 1, userId: 1);
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Released);

        await CreateController(db, userId: 1).SlettRessurs(1);

        Assert.That(db.Resources.Count(), Is.EqualTo(1));
    }
}