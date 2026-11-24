using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.DataAccess;

public sealed class ResourceRepository(AppDbContext db) : IResourceRepository
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

    public Task<List<Resource>> ListOwnedAsync(int ownerId) => db.Resources
        .AsNoTracking()
        .Include(resource => resource.Category)
        .Include(resource => resource.Vehicle)
        .Include(resource => resource.Equipment)
        .Where(resource => resource.BrukerId == ownerId)
        .OrderByDescending(resource => resource.ResourceId)
        .ToListAsync();

    public Task<bool> CategoryExistsAsync(int? categoryId) =>
        db.Categories.AnyAsync(category => category.CategoryId == categoryId);

    public async Task AddAsync(Resource resource)
    {
        db.Resources.Add(resource);
        await db.SaveChangesAsync();
    }

    public Task<Resource?> FindOwnedAsync(int id, int ownerId, bool includeDetails = false)
    {
        IQueryable<Resource> query = db.Resources;
        if (includeDetails)
            query = query.Include(resource => resource.Vehicle).Include(resource => resource.Equipment);

        return query.SingleOrDefaultAsync(resource =>
            resource.ResourceId == id && resource.BrukerId == ownerId);
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public async Task<bool> TryDeleteAsync(Resource resource)
    {
        if (await HasAssignmentAsync(resource.ResourceId))
            return false;

        db.Resources.Remove(resource);
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            if (!await HasAssignmentAsync(resource.ResourceId))
                throw;

            return false;
        }
    }

    private Task<bool> HasAssignmentAsync(int resourceId) =>
        db.ResourceAssignments.AnyAsync(assignment => assignment.ResourceId == resourceId);

    public Task<List<Resource>> ListOwnedWithCategoryAsync(int ownerId) => db.Resources
        .AsNoTracking()
        .Include(resource => resource.Category)
        .Where(resource => resource.BrukerId == ownerId)
        .OrderByDescending(resource => resource.ResourceId)
        .ToListAsync();
}
