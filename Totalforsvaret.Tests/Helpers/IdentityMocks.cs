using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Totalforsvaret.Models;

namespace Totalforsvaret.Tests;

internal static class IdentityMocks
{
    public static Mock<UserManager<Bruker>> CreateUsers() => new(
        Mock.Of<IUserStore<Bruker>>(),
        Options.Create(new IdentityOptions()),
        new PasswordHasher<Bruker>(),
        Array.Empty<IUserValidator<Bruker>>(),
        Array.Empty<IPasswordValidator<Bruker>>(),
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(),
        Mock.Of<IServiceProvider>(),
        NullLogger<UserManager<Bruker>>.Instance);

    public static Mock<SignInManager<Bruker>> CreateSignIn(UserManager<Bruker> users) => new(
        users,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        Mock.Of<IUserClaimsPrincipalFactory<Bruker>>(),
        Options.Create(new IdentityOptions()),
        NullLogger<SignInManager<Bruker>>.Instance,
        Mock.Of<IAuthenticationSchemeProvider>(),
        Mock.Of<IUserConfirmation<Bruker>>());
}
