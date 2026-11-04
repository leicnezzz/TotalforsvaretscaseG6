using Microsoft.EntityFrameworkCore;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.Handlers;

public class ResourceHandler(AppDbContext db)
{
    // Henter kategoriene som migrasjonen opprettet i databasen.
    public IReadOnlyList<Category> GetCategories()
    {
        return db.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryId)
            .ToList();
    }

    // Lagrer en ressurs med fremmednøkkel til Identity-brukeren.
    public Resource CreateResource(
        string resourceName,
        int categoryId,
        bool available,
        decimal latitude,
        decimal longitude,
        int brukerId,
        string navn = "",
        string kontaktpunkt = "",
        DateTime? tilgjengeligFra = null)
    {
        if (brukerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(brukerId));
        }

        var category = db.Categories
            .SingleOrDefault(category =>
                category.CategoryId == categoryId);

        if (category is null)
        {
            throw new ArgumentException(
                "Kategorien finnes ikke.",
                nameof(categoryId));
        }

        var resource = new Resource
        {
            BrukerId = brukerId,
            Object = resourceName,
            CategoryId = category.CategoryId,
            Category = category,
            Available = available,
            Latitude = latitude,
            Longitude = longitude,
            Navn = navn,
            Kontaktpunkt = kontaktpunkt,
            TilgjengeligFra = tilgjengeligFra
        };

        db.Resources.Add(resource);
        db.SaveChanges();

        return resource;
    }

    // Henter kartpunktene til den innloggede brukeren.
    public IReadOnlyList<ResourceMapPoint> GetResourcePointsForUser(
        int brukerId)
    {
        if (brukerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(brukerId));
        }

        return db.Resources
            .AsNoTracking()
            .Where(resource => resource.BrukerId == brukerId)
            .OrderBy(resource => resource.ResourceId)
            .Select(resource => new ResourceMapPoint(
                resource.ResourceId,
                resource.Object,
                resource.Latitude,
                resource.Longitude))
            .ToList();
    }
}