using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Totalforsvaret.Controllers;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using System.Text.Json;

namespace Totalforsvaret.Tests;

public class CrisisMapAnnotationsTests
{
    private AppDbContext db = null!;
    private CrisisMapAnnotationsController controller = null!;

    [SetUp]
    public void SetUp()
    {
        db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Crises.AddRange(new Crisis { Id = 1, Name = "Flom" }, new Crisis { Id = 2, Name = "Brann" });
        db.SaveChanges();
        controller = new CrisisMapAnnotationsController(db);
    }

    [TearDown] public void TearDown() => db.Dispose();

    private static CrisisMapAnnotationsController.AnnotationInput Road() => new()
    {
        Kind = "veihindringer", Name = " Stengt vei ", Impact = "Høy",
        Coordinates = [[58.14, 8.0]]
    };

    [Test]
    public async Task SavesRoadToSelectedCrisisAndCanReloadIt()
    {
        Assert.That(await controller.Create(2, Road()), Is.TypeOf<OkObjectResult>());
        db.ChangeTracker.Clear();
        var saved = await db.CrisisMapAnnotations.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(saved.CrisisId, Is.EqualTo(2));
            Assert.That(saved.Name, Is.EqualTo("Stengt vei"));
            Assert.That(JsonSerializer.Deserialize<double[][]>(saved.CoordinatesJson)![0][0], Is.EqualTo(58.14));
        });
    }

    [Test]
    public async Task SavesIndependentAnnotationsForTwoCrises()
    {
        await controller.Create(1, Road());
        await controller.Create(2, Road());
        db.ChangeTracker.Clear();
        Assert.That(await db.CrisisMapAnnotations.CountAsync(a => a.CrisisId == 1), Is.EqualTo(1));
        Assert.That(await db.CrisisMapAnnotations.CountAsync(a => a.CrisisId == 2), Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(99)]
    public async Task RejectsMissingCrisis(int crisisId)
    {
        Assert.That(await controller.Create(crisisId, Road()), Is.TypeOf<NotFoundResult>());
        Assert.That(await db.CrisisMapAnnotations.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task CannotDeleteViaWrongCrisis()
    {
        await controller.Create(1, Road());
        var id = (await db.CrisisMapAnnotations.SingleAsync()).Id;
        Assert.That(await controller.Delete(2, id), Is.TypeOf<NotFoundResult>());
        Assert.That(await db.CrisisMapAnnotations.CountAsync(), Is.EqualTo(1));
        Assert.That(await controller.Delete(1, id), Is.TypeOf<NoContentResult>());
        Assert.That(await db.CrisisMapAnnotations.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task RejectsInvalidCoordinates()
    {
        var input = Road();
        input.Coordinates = [[91, 8]];
        Assert.That(await controller.Create(1, input), Is.TypeOf<BadRequestObjectResult>());
        Assert.That(await db.CrisisMapAnnotations.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task RejectsZoneWithoutArea()
    {
        var input = Road();
        input.Kind = "soner";
        input.Risk = "hoy";
        input.Coordinates = [[58, 8], [58, 9], [58, 10]];
        Assert.That(await controller.Create(1, input), Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SavesZoneAndRisk()
    {
        var input = Road();
        input.Kind = "soner";
        input.Risk = "moderat";
        input.Coordinates = [[58, 8], [58.1, 8], [58, 8.1]];
        Assert.That(await controller.Create(1, input), Is.TypeOf<OkObjectResult>());
        db.ChangeTracker.Clear();
        Assert.That((await db.CrisisMapAnnotations.SingleAsync()).Risk, Is.EqualTo("moderat"));
    }
}
