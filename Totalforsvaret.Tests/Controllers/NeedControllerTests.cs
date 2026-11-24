using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Controllers;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.DataAccess;
using static Totalforsvaret.Tests.Helpers.PublicActorTestHelper;

namespace Totalforsvaret.Tests.Controllers;

/// <summary>
/// Unit tests for NeedController in the Offentlig area.
/// </summary>
public class NeedControllerTests
{
    /// <summary>
    /// Creates the controller with a signed-in user.
    /// </summary>
    private static NeedController CreateController(AppDbContext db)
    {
        // UserManager is only used by Create, which is not tested here.
        return WithUser(new NeedController(db, null!));
    }

    /// <summary>
    /// Deleting a need that does not exist returns NotFound.
    /// </summary>
    [Test]
    public async Task Delete_NeedNotFound_ReturnsNotFound()
    {
        using var db = CreateDb();

        var result = await CreateController(db).Delete(99);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    /// <summary>
    /// Deleting a need also removes the assignments that belong to it.
    /// </summary>
    [Test]
    public async Task Delete_ExistingNeed_DeletesNeedAndAssignments()
    {
        using var db = CreateDb();
        AddNeed(db, 1);
        AddAssignment(db, 1, needId: 1, resourceId: 1, AssignmentStatus.Requested);

        var result = await CreateController(db).Delete(1);

        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        Assert.That(db.Needs, Is.Empty);
        Assert.That(db.ResourceAssignments, Is.Empty);
    }

    /// <summary>
    /// Editing a need that does not exist returns NotFound.
    /// </summary>
    [Test]
    public async Task Edit_NeedNotFound_ReturnsNotFound()
    {
        using var db = CreateDb();

        var result = await CreateController(db).Edit(99, new NeedCreateViewModel());

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}