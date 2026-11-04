using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Privat.Controllers;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Handlers;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Tests;

public class ResourceControllerTests
{
    private AppDbContext db = null!;
    private ResourceController controller = null!;
    private Mock<UserManager<Bruker>> users = null!;
    private readonly Bruker owner = new Privataktor { Id = 10, Navn = "Testbruker" };

    [SetUp]
    public void SetUp()
    {
        // Hver test har sin egen database. Ingen Docker eller MariaDB er nødvendig.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new AppDbContext(options);
        db.Database.EnsureCreated();
        users = IdentityMocks.CreateUsers();
        users.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(owner);
        controller = new ResourceController(db, new ResourceHandler(db), users.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TearDown]
    public void TearDown()
    {
        controller.Dispose();
        db.Dispose();
        users.Object.Dispose();
    }

    private static ResourceInput VehicleInput() => new()
    {
        Object = " Varebil ", CategoryId = 1, Available = true,
        Kontaktpunkt = " 90012345 ", Address = " Rådhusgata 18 ",
        Latitude = 58.1467m, Longitude = 7.9956m, Condition = " God ",
        Vehicle = new VehicleInput { RegistrationNumber = " ab12345 ", RequiredLicense = " B " }
    };

    private static ResourceInput EquipmentInput() => new()
    {
        Object = "Drone", CategoryId = 2, Available = false,
        Kontaktpunkt = "90012345", Address = "Rådhusgata 18",
        Latitude = 58.1467m, Longitude = 7.9956m, Condition = "God",
        Equipment = new EquipmentInput { EquipmentType = "Drone", Quantity = 2 }
    };

    private async Task<int> SeedVehicle(int userId, string name = "Opprinnelig")
    {
        var resource = new Resource
        {
            BrukerId = userId, Object = name, CategoryId = 1,
            Vehicle = new VehicleDetails { RegistrationNumber = "CD54321", RequiredLicense = "T" }
        };
        db.Resources.Add(resource);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return resource.ResourceId;
    }

    [Test]
    public async Task CreateVehicle_UsesIdentityOwnerAndSavesVehicleDetails()
    {
        var result = await controller.Create(VehicleInput());
        db.ChangeTracker.Clear();
        var saved = await db.Resources.Include(resource => resource.Vehicle).SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(saved.BrukerId, Is.EqualTo(owner.Id));
            Assert.That(saved.Object, Is.EqualTo("Varebil"));
            Assert.That(saved.Vehicle!.RegistrationNumber, Is.EqualTo("AB12345"));
            Assert.That(saved.Vehicle.RequiredLicense, Is.EqualTo("B"));
            Assert.That(saved.Equipment, Is.Null);
        });
    }

    [Test]
    public async Task CreateEquipment_SavesQuantityAndOptionalAvailabilityDate()
    {
        var input = EquipmentInput();
        input.TilgjengeligFra = new DateTime(2027, 1, 1, 12, 0, 0);
        await controller.Create(input);
        db.ChangeTracker.Clear();
        var saved = await db.Resources.Include(resource => resource.Equipment).SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Equipment!.EquipmentType, Is.EqualTo("Drone"));
            Assert.That(saved.Equipment.Quantity, Is.EqualTo(2));
            Assert.That(saved.Available, Is.False);
            Assert.That(saved.TilgjengeligFra, Is.EqualTo(input.TilgjengeligFra));
            Assert.That(saved.Vehicle, Is.Null);
        });
    }

    [Test]
    public async Task CreateVehicle_NullOptionalFieldsAreSavedAsEmptyStrings()
    {
        var input = VehicleInput();
        input.AdditionalInformation = null;
        input.Vehicle!.RequiredLicense = null;
        input.Vehicle.RegistrationNumber = null;
        await controller.Create(input);
        db.ChangeTracker.Clear();
        var saved = await db.Resources.Include(resource => resource.Vehicle).SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.AdditionalInformation, Is.Empty);
            Assert.That(saved.Vehicle!.RequiredLicense, Is.Empty);
            Assert.That(saved.Vehicle.RegistrationNumber, Is.Null);
        });
    }

    [TestCase("Create")]
    [TestCase("Update")]
    public async Task InvalidModelState_DoesNotWriteResources(string action)
    {
        // MVC-validering kjøres før handlingen i appen. Simuler feil i ModelState.
        controller.ModelState.AddModelError("Object", "Navn mangler.");
        var result = action == "Create"
            ? await controller.Create(VehicleInput())
            : await controller.Update(1, VehicleInput());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var problem = ((ObjectResult)result).Value as ValidationProblemDetails;
            Assert.That(problem, Is.Not.Null);
            Assert.That(problem!.Errors["Object"], Does.Contain("Navn mangler."));
            Assert.That(db.Resources.Count(), Is.Zero);
        });
    }

    [TestCase("Create")]
    [TestCase("Update")]
    [TestCase("Delete")]
    [TestCase("Oversikt")]
    public async Task MissingIdentityUser_RequiresLogin(string action)
    {
        users.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((Bruker?)null);
        var result = action switch
        {
            "Create" => await controller.Create(VehicleInput()),
            "Update" => await controller.Update(1, VehicleInput()),
            "Delete" => await controller.Delete(1),
            _ => await controller.Oversikt()
        };
        Assert.That(result, Is.TypeOf<ChallengeResult>());
        Assert.That(await db.Resources.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Oversikt_ReturnsOnlyCurrentUsersResourcesNewestFirst()
    {
        var first = await SeedVehicle(owner.Id, "Egen første");
        await SeedVehicle(20, "Annen brukers ressurs");
        var last = await SeedVehicle(owner.Id, "Egen siste");
        var result = (ViewResult)await controller.Oversikt();
        var resources = (IReadOnlyList<Resource>)result.Model!;

        Assert.That(resources.Select(resource => resource.ResourceId), Is.EqualTo(new[] { last, first }));
    }

    [Test]
    public async Task UpdateOtherUsersResource_ReturnsNotFoundAndPreservesData()
    {
        var id = await SeedVehicle(20);
        var result = await controller.Update(id, VehicleInput());
        db.ChangeTracker.Clear();
        var saved = await db.Resources.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<NotFoundResult>());
            Assert.That(saved.Object, Is.EqualTo("Opprinnelig"));
            Assert.That(saved.BrukerId, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task DeleteOtherUsersResource_ReturnsNotFoundAndKeepsResource()
    {
        var id = await SeedVehicle(20);
        var result = await controller.Delete(id);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
        Assert.That(await db.Resources.AnyAsync(resource => resource.ResourceId == id), Is.True);
    }

    [Test]
    public async Task UpdateOwnResource_PreservesOwnerAndUpdatesExistingDetails()
    {
        var id = await SeedVehicle(owner.Id);
        var result = await controller.Update(id, VehicleInput());
        db.ChangeTracker.Clear();
        var saved = await db.Resources.Include(resource => resource.Vehicle).SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(saved.BrukerId, Is.EqualTo(owner.Id));
            Assert.That(saved.Object, Is.EqualTo("Varebil"));
            Assert.That(saved.Vehicle!.RegistrationNumber, Is.EqualTo("AB12345"));
            Assert.That(db.VehicleDetails.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateCategory_IsRejectedAndPreservesOriginalResource()
    {
        var id = await SeedVehicle(owner.Id);
        var result = await controller.Update(id, EquipmentInput());
        db.ChangeTracker.Clear();
        var saved = await db.Resources.SingleAsync();

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That(saved.CategoryId, Is.EqualTo(1));
        Assert.That(saved.Object, Is.EqualTo("Opprinnelig"));
    }

    [Test]
    public async Task DeleteOwnResource_RemovesResource()
    {
        var id = await SeedVehicle(owner.Id);
        var result = await controller.Delete(id);
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        Assert.That(await db.Resources.AnyAsync(resource => resource.ResourceId == id), Is.False);
    }
}
