using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Controllers;
using Totalforsvaret.Models;

namespace Totalforsvaret.Tests;

public class AccountControllerTests
{
    private Mock<UserManager<Bruker>> users = null!;
    private AccountController controller = null!;

    [SetUp]
    public void SetUp()
    {
        var store = new Mock<IUserStore<Bruker>>();

        users = new Mock<UserManager<Bruker>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<Bruker>(),
            Array.Empty<IUserValidator<Bruker>>(),
            Array.Empty<IPasswordValidator<Bruker>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<Bruker>>.Instance);

        // Disse testene bruker bare registrering, ikke SignInManager.
        controller = new AccountController(users.Object, null!);

        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    [TearDown]
    public void TearDown()
    {
        controller.Dispose();
        users.Object.Dispose();
    }

    [Test]
    public async Task Register_GyldigSkjema_OppretterPrivatBruker()
    {
        // Arrange
        var input = LagGyldigInput();

        users
            .Setup(manager => manager.CreateAsync(
                It.IsAny<Bruker>(),
                input.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var resultat = await controller.Register(input);

        // Assert: riktig brukertype og data sendes til Identity.
        users.Verify(
            manager => manager.CreateAsync(
                It.Is<Bruker>(bruker =>
                    bruker is Privataktor &&
                    bruker.UserName == input.Email &&
                    bruker.Email == input.Email &&
                    bruker.Navn == "Lucas Test"),
                input.Password),
            Times.Once);

        Assert.That(resultat, Is.TypeOf<RedirectToActionResult>());

        var redirect = (RedirectToActionResult)resultat;

        Assert.That(
            redirect.ActionName,
            Is.EqualTo(nameof(AccountController.Login)));

        Assert.That(
            controller.TempData["AccountMessage"],
            Is.Not.Null);
    }

    [Test]
    public async Task Register_EpostFinnesFraFor_ViserFeil()
    {
        // Arrange: simuler at Identity avviser en duplikatadresse.
        var input = LagGyldigInput();

        users
            .Setup(manager => manager.CreateAsync(
                It.IsAny<Bruker>(),
                input.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "E-postadressen er allerede registrert."
            }));

        // Act
        var resultat = await controller.Register(input);

        // Assert: skjemaet vises igjen med feilmelding.
        Assert.That(resultat, Is.TypeOf<ViewResult>());

        var view = (ViewResult)resultat;

        Assert.That(view.Model, Is.SameAs(input));

        Assert.That(
            controller.ViewData["FormError"],
            Is.EqualTo("E-postadressen er allerede registrert."));

        Assert.That(
            controller.TempData["AccountMessage"],
            Is.Null);

        users.Verify(
            manager => manager.CreateAsync(
                It.IsAny<Bruker>(),
                input.Password),
            Times.Once);
    }

    [Test]
    public async Task Register_UgyldigSkjema_OppretterIkkeBruker()
    {
        // Arrange
        var input = LagGyldigInput();
        input.Email = "";

        // MVC validerer normalt før controlleren kalles.
        // Ved direkte metodekall må testen legge inn feilen selv.
        controller.ModelState.AddModelError(
            nameof(RegisterInput.Email),
            "E-post er påkrevd.");

        // Act
        var resultat = await controller.Register(input);

        // Assert: ingen opprettelse forsøkes.
        users.Verify(
            manager => manager.CreateAsync(
                It.IsAny<Bruker>(),
                It.IsAny<string>()),
            Times.Never);

        Assert.That(resultat, Is.TypeOf<ViewResult>());

        var view = (ViewResult)resultat;

        Assert.That(view.Model, Is.SameAs(input));

        Assert.That(
            controller.ViewData["FormError"],
            Is.Not.Null);

        Assert.That(
            controller.TempData["AccountMessage"],
            Is.Null);
    }

    private static RegisterInput LagGyldigInput()
    {
        return new RegisterInput
        {
            FirstName = "Lucas",
            LastName = "Test",
            Email = "lucas@example.test",
            Password = "Testpassord123!",
            ConfirmPassword = "Testpassord123!"
        };
    }
}