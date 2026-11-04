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
        
        // Krisene som skal lagres i databasen.
        public DbSet<Crisis> Crises { get; set; }
        public DbSet<Need> Needs { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Konfigurer arv for Bruker-klassen
            builder.Entity<Bruker>()
                .HasDiscriminator<string>("BrukerType")
                .HasValue<Privataktor>("Privat")
                .HasValue<Offentligaktør>("Offentlig");
            
            // Konfigurer koblingen mellom krise og behov.
            builder.Entity<Need>()
                .HasOne(n => n.Crisis)
                .WithMany(c => c.Needs)
                .HasForeignKey(n => n.CrisisId); 
            
            // Konfigurer koblingen mellom behov og offentlig aktør.
            builder.Entity<Need>()
                .Ignore(n => n.OffentligAktor);
            
            // Konfigurer koblingen mellom krise og offentlig aktør.
            builder.Entity<Crisis>()
                .HasOne(c => c.OffentligAktor)
                .WithMany()
                .HasForeignKey(c => c.OffentligAktorId);
        }
    }
}