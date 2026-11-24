using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.DataAccess;

public interface IResourceRepository
{
    IReadOnlyList<Category> GetCategories();
    List<Resource> GetResources();
    IReadOnlyList<ResourceMapPoint> GetResourcePointsForUser(int userId);
    Task<List<Resource>> ListOwnedAsync(int ownerId);
    Task<bool> CategoryExistsAsync(int? categoryId);
    Task AddAsync(Resource resource);
    Task<Resource?> FindOwnedAsync(int id, int ownerId, bool includeDetails = false);
    Task SaveChangesAsync();
    Task<bool> TryDeleteAsync(Resource resource);
    Task<List<Resource>> ListOwnedWithCategoryAsync(int ownerId);
}