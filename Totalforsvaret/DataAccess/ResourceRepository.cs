using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.DataAccess;

public sealed class ResourceRepository : IResourceRepository {
    private readonly AppDbContext _db;

    public ResourceRepository(AppDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<Category> GetCategories()
    {
        return _db.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryId)
            .ToList();
    }

    public List<Resource> GetResources()
    {
        return _db.Resources
		.Include(r => r.Category)
			.AsNoTracking()
            .ToList();
    }

    public IReadOnlyList<ResourceMapPoint> GetResourcePointsForUser(
        int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        return _db.Resources
            .AsNoTracking()
            .Where(resource => resource.BrukerId == userId)
            .OrderBy(resource => resource.ResourceId)
            .Select(resource => new ResourceMapPoint(
                resource.ResourceId,
                resource.Object,
                resource.Latitude,
                resource.Longitude))
            .ToList();
    }

    public Task<List<Resource>> ListOwnedAsync(int ownerId) => _db.Resources
        .AsNoTracking()
        .Include(resource => resource.Category)
        .Include(resource => resource.Vehicle)
        .Include(resource => resource.Equipment)
        .Where(resource => resource.BrukerId == ownerId)
        .OrderByDescending(resource => resource.ResourceId)
        .ToListAsync();

    public Task<bool> CategoryExistsAsync(int? categoryId) =>
        _db.Categories.AnyAsync(category => category.CategoryId == categoryId);

    public async Task AddAsync(Resource resource)
    {
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();
    }

    public Task<Resource?> FindOwnedAsync(int id, int ownerId, bool includeDetails = false)
    {
        IQueryable<Resource> query = _db.Resources;
        if (includeDetails)
            query = query.Include(resource => resource.Vehicle).Include(resource => resource.Equipment);

        return query.SingleOrDefaultAsync(resource =>
            resource.ResourceId == id && resource.BrukerId == ownerId);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<bool> TryDeleteAsync(Resource resource)
    {
        if (await HasAssignmentAsync(resource.ResourceId))
            return false;

        _db.Resources.Remove(resource);
        try
        {
            await _db.SaveChangesAsync();
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
        _db.ResourceAssignments.AnyAsync(assignment => assignment.ResourceId == resourceId);
}
