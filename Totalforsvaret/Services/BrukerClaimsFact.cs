using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Services;
// Utvider brukerens claims med en claim som heter "Brukertype", den angir brukertype med verdien "Privat" eller "Offentlig".
// Claimen brukes for å bestemme hvilken aktørtype som er logget inn, og hvilke funksjoner som er tilgjengelig for brukeren.
// BrukerClaimsFactory registreres som en tjeneste i Program.cs
public class BrukerClaimsFactory : UserClaimsPrincipalFactory<Bruker>
{
    public BrukerClaimsFactory(
        UserManager<Bruker> users,
        IOptions<IdentityOptions> options)
        : base(users, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(
        Bruker bruker)
    {
        var identity = await base.GenerateClaimsAsync(bruker);

        var brukertype = bruker switch
        {
            Privataktor => "Privat",
            Offentligaktør => "Offentlig",
            _ => throw new InvalidOperationException("Ukjent brukertype.")
        };

        foreach (var claim in identity.FindAll("Brukertype").ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim("Brukertype", brukertype));

        return identity;
    }
}