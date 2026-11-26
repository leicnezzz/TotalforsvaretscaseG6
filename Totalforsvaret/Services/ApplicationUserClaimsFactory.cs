using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Services;

/// <summary>
/// Adds the actor type claim used by the application's authorization policies.
/// </summary>
public class ApplicationUserClaimsFactory : UserClaimsPrincipalFactory<Bruker>
{
    public ApplicationUserClaimsFactory(
        UserManager<Bruker> users,
        IOptions<IdentityOptions> options)
        : base(users, options)
    {
    }

    /// <summary>
    /// Adds the actor type claim based on the account subtype.
    /// </summary>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(
        Bruker user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var actorType = user switch
        {
            PrivateActor => "Privat",
            PublicActor => "Offentlig",
            _ => throw new InvalidOperationException("Ukjent actorType.")
        };

        foreach (var claim in identity.FindAll("Brukertype").ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim("Brukertype", actorType));

        return identity;
    }
}