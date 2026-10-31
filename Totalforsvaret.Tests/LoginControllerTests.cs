using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Controllers;
using Totalforsvaret.Models;

namespace Totalforsvaret.Tests;

public class LoginControllerTests
{
    private Mock<UserManager<Bruker>> users = null!;
    private Mock<SignInManager<Bruker>> signIn = null!;
    private AccountController controller = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new IdentityOptions());

        users = new Mock<UserManager<Bruker>>(
            Mock.Of<IUserStore<Bruker>>(),
            options,
            new PasswordHasher<Bruker>(),
            Array.Empty<IUserValidator<Bruker>>(),
            Array.Empty<IPasswordValidator<Bruker>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<Bruker>>.Instance);

        signIn = new Mock<SignInManager<Bruker>>(
            users.Object,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            },
            Mock.Of<IUserClaimsPrincipalFactory<Bruker>>(),
            options,
            NullLogger<SignInManager<Bruker>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            new DefaultUserConfirmation<Bruker>());

        controller = new AccountController(users.Object, signIn.Object);
    }

    [TearDown]
    public void TearDown()
    {
        controller.Dispose();
        users.Object.Dispose();
    }

    [TestCase("civil", false)]
    [TestCase("public", true)]
    public async Task Login_RiktigPassordOgAktortype_SenderTilForsiden(
        string actor,
        bool offentlig)
    {
        // Arrange
        var input = LagInput(actor);
        input.RememberMe = true;

        Bruker bruker = offentlig
            ? new Offentligaktør()
            : new Privataktor();

        users.Setup(manager => manager.FindByEmailAsync(input.Email))
            .ReturnsAsync(bruker);

        signIn.Setup(manager => manager.PasswordSignInAsync(
                bruker, input.Password, true, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var resultat = await controller.Login(input);

        // Assert
        Assert.That(resultat, Is.TypeOf<RedirectToActionResult>());

        var redirect = (RedirectToActionResult)resultat;

        Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        Assert.That(redirect.ControllerName, Is.EqualTo("Home"));

        // Husk meg og kontolåsing videresendes til Identity.
        signIn.Verify(manager => manager.PasswordSignInAsync(
                bruker, input.Password, true, true),
            Times.Once);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Login_AvvistAvIdentity_ViserFeil(bool lockedOut)
    {
        var input = LagInput();
        var bruker = new Privataktor();

        users.Setup(manager => manager.FindByEmailAsync(input.Email))
            .ReturnsAsync(bruker);

        var identityResult = lockedOut
            ? Microsoft.AspNetCore.Identity.SignInResult.LockedOut
            : Microsoft.AspNetCore.Identity.SignInResult.Failed;

        signIn.Setup(manager => manager.PasswordSignInAsync(
                bruker, input.Password, false, true))
            .ReturnsAsync(identityResult);

        var resultat = await controller.Login(input);

        KontrollerLoginFeil(resultat, input);
    }

    [TestCase("public")]
    [TestCase("ukjent")]
    public async Task Login_FeilAktortype_ForsokerIkkeInnlogging(
        string actor)
    {
        var input = LagInput(actor);

        users.Setup(manager => manager.FindByEmailAsync(input.Email))
            .ReturnsAsync(new Privataktor());

        var resultat = await controller.Login(input);

        KontrollerLoginFeil(resultat, input);
        KontrollerIngenInnlogging();
    }

    [Test]
    public async Task Login_UkjentBruker_ForsokerIkkeInnlogging()
    {
        var input = LagInput();

        users.Setup(manager => manager.FindByEmailAsync(input.Email))
            .ReturnsAsync((Bruker?)null);

        var resultat = await controller.Login(input);

        KontrollerLoginFeil(resultat, input);
        KontrollerIngenInnlogging();
    }

    [Test]
    public async Task Login_UgyldigSkjema_SlArIkkeOppBruker()
    {
        var input = LagInput();

        // MVC utfører normalt denne valideringen før metodekallet.
        controller.ModelState.AddModelError(
            nameof(LoginInput.Email),
            "Ugyldig e-post.");

        var resultat = await controller.Login(input);

        KontrollerLoginFeil(resultat, input);

        users.Verify(manager =>
            manager.FindByEmailAsync(It.IsAny<string>()),
            Times.Never);

        KontrollerIngenInnlogging();
    }

    private void KontrollerLoginFeil(
        IActionResult resultat,
        LoginInput input)
    {
        Assert.That(resultat, Is.TypeOf<ViewResult>());

        var view = (ViewResult)resultat;

        Assert.That(view.ViewName, Is.EqualTo("Login"));
        Assert.That(view.Model, Is.SameAs(input));
        Assert.That(controller.ViewData["FormError"], Is.Not.Null);
    }

    private void KontrollerIngenInnlogging()
    {
        signIn.Verify(manager => manager.PasswordSignInAsync(
                It.IsAny<Bruker>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    private static LoginInput LagInput(string actor = "civil")
    {
        return new LoginInput
        {
            Email = "bruker@example.test",
            Password = "Testpassord123!",
            Actor = actor,
            RememberMe = false
        };
    }
}