using Microsoft.AspNetCore.Identity;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;


public static class TestUserSeeder
{
    /// <summary>
    /// Creates the public demonstration account if it does not already exist.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<Bruker>>();

        const string email = "offentlig@example.com";

        var eksisterende = await users.FindByEmailAsync(email);

        if (eksisterende != null)
        {
            if (eksisterende is not PublicActor)
            {
                throw new InvalidOperationException(
                    "Testkontoens e-post er allerede brukt av en privat konto.");
            }

            return;
        }

        var user = new PublicActor
        {
            UserName = email,
            Email = email,
            Navn = "Offentlig Demo",
            Avdeling = "Test",
            EmailConfirmed = true
        };


        var resultat = await users.CreateAsync(
            user,
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