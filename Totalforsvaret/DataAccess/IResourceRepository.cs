using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.DataAccess;

public interface IResourceRepository
{
    IReadOnlyList<Category> GetCategories();
    List<Resource> GetResources();
    IReadOnlyList<ResourceMapPoint> GetResourcePointsForUser(int userId);

    /// <summary>
    /// Lists the owner's resources with their category and vehicle or equipment details.
    /// </summary>
    Task<List<Resource>> ListOwnedAsync(int ownerId);
    Task<bool> CategoryExistsAsync(int? categoryId);
    Task AddAsync(Resource resource);

    /// <summary>
    /// Loads vehicle or equipment details when includeDetails is true.
    /// </summary>
    Task<Resource?> FindOwnedAsync(int id, int ownerId, bool includeDetails = false);
    Task SaveChangesAsync();

    /// <summary>
    /// Deletes a resource when it has no assignment history and returns false when an assignment
    /// prevents deletion.
    /// </summary>
    Task<bool> TryDeleteAsync(Resource resource);

    Task<List<Resource>> ListOwnedWithCategoryAsync(int ownerId);
}