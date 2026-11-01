using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Services;

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