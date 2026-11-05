using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.DataAccess;

public class AppDbContext : IdentityUserContext<Bruker, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Crisis> Crises => Set<Crisis>();
    public DbSet<Need> Needs => Set<Need>();
    public DbSet<ResourceAssignment> ResourceAssignments
        => Set<ResourceAssignment>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<VehicleDetails> VehicleDetails =>
        Set<VehicleDetails>();

    public DbSet<EquipmentDetails> EquipmentDetails =>
        Set<EquipmentDetails>();
    
    public DbSet<EgenberedskapChecklistStatus> EgenberedskapChecklistStatuses =>
        Set<EgenberedskapChecklistStatus>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ResourceAssignment>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.Need)
                .WithMany()
                .HasForeignKey(a => a.NeedId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Resource)
                .WithMany()
                .HasForeignKey(a => a.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.ResourceId, a.Status });
            entity.HasIndex(a => new { a.NeedId, a.Status });
        });

        builder.Entity<Bruker>()
            .HasDiscriminator<string>("BrukerType")
            .HasValue<Privataktor>("Privat")
            .HasValue<Offentligaktør>("Offentlig");

        builder.Entity<Need>()
            .HasOne(n => n.Crisis)
            .WithMany(c => c.Needs)
            .HasForeignKey(n => n.CrisisId);

        builder.Entity<Need>()
            .Ignore(n => n.OffentligAktor);

        builder.Entity<Crisis>()
            .HasOne(c => c.OffentligAktor)
            .WithMany()
            .HasForeignKey(c => c.OffentligAktorId);

        builder.Entity<Resource>(entity =>
        {
            entity.HasKey(resource => resource.ResourceId);

            entity.Property(resource => resource.Latitude)
                .HasPrecision(9, 6);

            entity.Property(resource => resource.Longitude)
                .HasPrecision(9, 6);

            // Knytter ressursen til den eksisterende Identity-brukeren.
            entity.HasOne(resource => resource.Bruker)
                .WithMany()
                .HasForeignKey(resource => resource.BrukerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(resource => resource.Category)
                .WithMany(category => category.Resources)
                .HasForeignKey(resource => resource.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VehicleDetails>(entity =>
        {
            entity.HasKey(details => details.ResourceId);

            entity.HasOne(details => details.Resource)
                .WithOne(resource => resource.Vehicle)
                .HasForeignKey<VehicleDetails>(
                    details => details.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EquipmentDetails>(entity =>
        {
            entity.HasKey(details => details.ResourceId);

            entity.HasOne(details => details.Resource)
                .WithOne(resource => resource.Equipment)
                .HasForeignKey<EquipmentDetails>(
                    details => details.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>().HasData(
            new Category
            {
                CategoryId = 1,
                Name = "Vehicles"
            },
            new Category
            {
                CategoryId = 2,
                Name = "Equipment"
            }
        );
        builder.Entity<EgenberedskapChecklistStatus>(entity =>
        {
            entity.HasKey(status => new { status.BrukerId, status.ItemId });
            entity.Property(status => status.ItemId).HasMaxLength(80);

            entity.HasOne<Bruker>()
                .WithMany()
                .HasForeignKey(status => status.BrukerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}