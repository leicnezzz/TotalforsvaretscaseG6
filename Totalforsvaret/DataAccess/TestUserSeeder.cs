using Microsoft.AspNetCore.Identity;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;


// Fomålet med denne er KUN og tilatte offentlig bruker til demoen for å slippe
// full administrasjons admin panel. 
public static class TestUserSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<Bruker>>();

        const string email = "offentlig@example.com";

        var eksisterende = await users.FindByEmailAsync(email);

        if (eksisterende != null)
        {
            if (eksisterende is not Offentligaktør)
            {
                throw new InvalidOperationException(
                    "Testkontoens e-post er allerede brukt av en privat konto.");
            }

            return;
        }

        var bruker = new Offentligaktør
        {
            UserName = email,
            Email = email,
            Navn = "Offentlig Demo",
            Avdeling = "Test",
            EmailConfirmed = true
        };


        // JA vi vett at dette er VELDIG VELDIG VELDIG sårbart men for test versjonen funker det :)
        var resultat = await users.CreateAsync(
            bruker,
            "Passordet12345");

        if (!resultat.Succeeded)
        {
            throw new InvalidOperationException(
                "Kunne ikke opprette offentlig testkonto: " +
                string.Join(
                    "; ",
                    resultat.Errors.Select(error => error.Description)));
        }
    }
}