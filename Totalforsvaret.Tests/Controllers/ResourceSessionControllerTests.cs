using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Home.Controllers;
using Totalforsvaret.Models;

namespace Totalforsvaret.Tests;

public class ResourceSessionControllerTests
{
    private Mock<UserManager<Bruker>> users = null!;
    private Mock<SignInManager<Bruker>> signIn = null!;
    private Mock<IAntiforgery> antiforgery = null!;
    private ResourceSessionController controller = null!;

    [SetUp]
    public void SetUp()
    {
        users = IdentityMocks.CreateUsers();
        signIn = IdentityMocks.CreateSignIn(users.Object);
        antiforgery = new Mock<IAntiforgery>();
        controller = new ResourceSessionController(users.Object, signIn.Object, antiforgery.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TearDown]
    public void TearDown()
    {
        controller.Dispose();
        users.Object.Dispose();
    }

    private static LoginInput LoginInput() => new()
    {
        Email = " test@example.com ", Password = "EtTestpassord123"
    };

    private static RegisterInput RegisterInput() => new()
    {
        FirstName = " Test ", LastName = " Bruker ", Email = " test@example.com ",
        Password = "EtTestpassord123", ConfirmPassword = "EtTestpassord123"
    };

    [Test]
    public async Task Register_CreatesPrivateIdentityUserBeforeSigningIn()
    {
        var input = RegisterInput();
        Bruker? created = null;
        var sequence = new MockSequence();
        users.InSequence(sequence)
            .Setup(manager => manager.CreateAsync(It.IsAny<Bruker>(), input.Password))
            .Callback<Bruker, string>((user, _) => created = user)
            .ReturnsAsync(IdentityResult.Success);
        signIn.InSequence(sequence)
            .Setup(manager => manager.SignInAsync(It.IsAny<Bruker>(), false, null))
            .Returns(Task.CompletedTask);

        var result = await controller.Register(input);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(created, Is.TypeOf<Privataktor>());
            Assert.That(created!.Email, Is.EqualTo("test@example.com"));
            Assert.That(created.UserName, Is.EqualTo(created.Email));
            Assert.That(created.Navn, Is.EqualTo("Test Bruker"));
        });
        signIn.Verify(manager => manager.SignInAsync(created!, false, null), Times.Once);
    }

    [Test]
    public async Task Register_IdentityFailureDoesNotSignIn()
    {
        users.Setup(manager => manager.CreateAsync(It.IsAny<Bruker>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail", Description = "E-postadressen er allerede brukt."
            }));

        var result = await controller.Register(RegisterInput());

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        signIn.Verify(manager => manager.SignInAsync(It.IsAny<Bruker>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [TestCase("Login")]
    [TestCase("Register")]
    public async Task InvalidModelState_DoesNotInvokeIdentity(string action)
    {
        controller.ModelState.AddModelError("Email", "Ugyldig e-postadresse.");
        var result = action == "Login"
            ? await controller.Login(LoginInput())
            : await controller.Register(RegisterInput());

        var problem = ((ObjectResult)result).Value as ValidationProblemDetails;
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Errors["Email"], Does.Contain("Ugyldig e-postadresse."));
        users.Verify(manager => manager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        users.Verify(manager => manager.CreateAsync(It.IsAny<Bruker>(), It.IsAny<string>()), Times.Never);
        signIn.Verify(manager => manager.PasswordSignInAsync(It.IsAny<Bruker>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task Login_UnknownEmailDoesNotAttemptSignIn()
    {
        users.Setup(manager => manager.FindByEmailAsync("test@example.com"))
            .ReturnsAsync((Bruker?)null);

        var result = await controller.Login(LoginInput());

        Assert.That(result, Is.TypeOf<UnauthorizedObjectResult>());
        signIn.Verify(manager => manager.PasswordSignInAsync(It.IsAny<Bruker>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task Login_PublicAccountCannotEnterPrivateRegistrationFlow()
    {
        users.Setup(manager => manager.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(new Offentligaktør { Navn = "Offentlig testbruker" });

        var result = await controller.Login(LoginInput());

        Assert.That(result, Is.TypeOf<UnauthorizedObjectResult>());
        signIn.Verify(manager => manager.PasswordSignInAsync(It.IsAny<Bruker>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Login_WrongPasswordOrLockedAccountIsRejected(bool lockedOut)
    {
        var user = new Privataktor { Navn = "Privat testbruker" };
        var input = LoginInput();
        users.Setup(manager => manager.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        signIn.Setup(manager => manager.PasswordSignInAsync(user, input.Password, false, true))
            .ReturnsAsync(lockedOut ? Microsoft.AspNetCore.Identity.SignInResult.LockedOut : Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await controller.Login(input);

        Assert.That(result, Is.TypeOf<UnauthorizedObjectResult>());
        signIn.Verify(manager => manager.PasswordSignInAsync(user, input.Password, false, true), Times.Once);
    }

    [Test]
    public async Task Login_ValidPrivateAccountUsesIdentityWithLockoutEnabled()
    {
        var user = new Privataktor { Navn = "Privat testbruker" };
        var input = LoginInput();
        users.Setup(manager => manager.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        signIn.Setup(manager => manager.PasswordSignInAsync(user, input.Password, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await controller.Login(input);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        signIn.Verify(manager => manager.PasswordSignInAsync(user, input.Password, false, true), Times.Once);
    }

    [Test]
    public void Token_ReturnsRequestTokenFromAntiforgeryService()
    {
        antiforgery.Setup(service => service.GetAndStoreTokens(controller.HttpContext))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "form-field", "header-name"));

        var result = (OkObjectResult)controller.Token();
        var json = JsonSerializer.SerializeToElement(result.Value);

        Assert.That(json.GetProperty("token").GetString(), Is.EqualTo("request-token"));
        antiforgery.Verify(service => service.GetAndStoreTokens(controller.HttpContext), Times.Once);
    }
}
