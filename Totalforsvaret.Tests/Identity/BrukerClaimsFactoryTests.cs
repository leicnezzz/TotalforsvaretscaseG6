using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;
using Totalforsvaret.Services;

namespace Totalforsvaret.Tests;

public class BrukerClaimsFactoryTests
{
    [TestCase(false, "Privat")]
    [TestCase(true, "Offentlig")]
    public async Task CreateAsync_GirRiktigBrukertypeClaim(
        bool offentlig,
        string forventetType)
    {
        // Arrange
        Bruker bruker = offentlig
            ? new Offentligaktør()
            : new Privataktor();

        bruker.Id = 42;
        bruker.UserName = "bruker@example.test";

        var options = Options.Create(new IdentityOptions());

        var users = new Mock<UserManager<Bruker>>(
            Mock.Of<IUserStore<Bruker>>(),
            options,
            new PasswordHasher<Bruker>(),
            Array.Empty<IUserValidator<Bruker>>(),
            Array.Empty<IPasswordValidator<Bruker>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<Bruker>>.Instance);

        using var manager = users.Object;

        users.Setup(m => m.GetUserIdAsync(bruker))
            .ReturnsAsync("42");

        users.Setup(m => m.GetUserNameAsync(bruker))
            .ReturnsAsync(bruker.UserName);

        var factory = new BrukerClaimsFactory(manager, options);

        // Act
        var principal = await factory.CreateAsync(bruker);

        // Assert: riktig brukertype, uten duplikater.
        var typeClaims = principal.FindAll("Brukertype").ToList();

        Assert.That(typeClaims, Has.Count.EqualTo(1));
        Assert.That(typeClaims[0].Value, Is.EqualTo(forventetType));

        // Identity sin bruker-ID skal fortsatt være med.
        Assert.That(
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Is.EqualTo("42"));
    }
}