using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Services;
// Utvider brukerens claims med en claim som heter "Brukertype", den angir actorType med verdien "Privat" eller "Offentlig".
// Claimen brukes for å bestemme hvilken aktørtype som er logget inn, og hvilke funksjoner som er tilgjengelig for brukeren.
// ApplicationUserClaimsFactory registreres som en tjeneste i Program.cs
public class ApplicationUserClaimsFactory : UserClaimsPrincipalFactory<Bruker>
{
    public ApplicationUserClaimsFactory(
        UserManager<Bruker> users,
        IOptions<IdentityOptions> options)
        : base(users, options)
    {
    }

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