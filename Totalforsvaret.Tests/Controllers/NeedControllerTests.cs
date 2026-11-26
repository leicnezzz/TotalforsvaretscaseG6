using Microsoft.AspNetCore.Mvc;
using Moq;
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
        return WithUser(new NeedController(new NeedRepository(db), new CrisisRepository(db), null!));
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

    [Test]
    public async Task Delete_ExistingNeed_DelegatesDeletionAndRedirects()
    {
        var need = new Need { Id = 1 };
        var needs = new Mock<INeedRepository>(MockBehavior.Strict);
        needs.Setup(repository => repository.FindAsync(1, true)).ReturnsAsync(need);
        needs.Setup(repository => repository.DeleteAsync(need)).Returns(Task.CompletedTask);
        using var controller = WithUser(new NeedController(needs.Object, Mock.Of<ICrisisRepository>(), null!));

        var result = await controller.Delete(1);

        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        needs.Verify(repository => repository.DeleteAsync(need), Times.Once);
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