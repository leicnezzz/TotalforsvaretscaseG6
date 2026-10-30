using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;


namespace Totalforsvaret.DataAccess
{
    public class AppDbContext : IdentityUserContext<Bruker, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Konfigurer arv for Bruker-klassen
            builder.Entity<Bruker>()
                .HasDiscriminator<string>("BrukerType")
                .HasValue<PrivatBruker>("Privat")
                .HasValue<OffentligBruker>("Offentlig");
        }
    }
}