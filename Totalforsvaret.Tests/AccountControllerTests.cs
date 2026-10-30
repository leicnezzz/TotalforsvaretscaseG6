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
    [Test]
    public async Task Register_GyldigSkjema_OppretterPrivatBruker()
    {
        // Arrange: erstatt databaseoperasjonene med en mock.
        var store = new Mock<IUserStore<Bruker>>();

        var users = new Mock<UserManager<Bruker>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<Bruker>(),
            Array.Empty<IUserValidator<Bruker>>(),
            Array.Empty<IPasswordValidator<Bruker>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<Bruker>>.Instance);

        users
            .Setup(manager => manager.CreateAsync(
                It.IsAny<Bruker>(),
                It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Registreringsmetoden bruker ikke SignInManager.
        var controller = new AccountController(users.Object, null!);

        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        var input = new RegisterInput
        {
            FirstName = "Lucas",
            LastName = "Test",
            Email = "lucas@example.test",
            Password = "Testpassord123!",
            ConfirmPassword = "Testpassord123!"
        };

        // Act: kall registreringsmetoden.
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

        // Etter registrering skal brukeren sendes til innlogging.
        Assert.That(resultat, Is.TypeOf<RedirectToActionResult>());

        var redirect = (RedirectToActionResult)resultat;

        Assert.That(
            redirect.ActionName,
            Is.EqualTo(nameof(AccountController.Login)));

        Assert.That(
            controller.TempData["AccountMessage"],
            Is.Not.Null);
    }
}

//test kommando dotnet test Totalforsvaret.Tests/Totalforsvaret.Tests.csproj