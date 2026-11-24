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

    // Henter alle registrerte ressurser til oversikten.
    public List<Resource> GetResources()
    {
        return db.Resources
		.Include(r => r.Category)
			.AsNoTracking()
            .ToList();
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