using Totalforsvaret.Models;
using Totalforsvaret.Models.resourcemodels;

namespace Totalforsvaret.DataAccess;

/// <summary>
/// Provides resource and category data for registration, matching and owner-managed changes.
/// </summary>
public interface IResourceRepository
{
    IReadOnlyList<Category> GetCategories();
    List<Resource> GetResources();
    IReadOnlyList<ResourceMapPoint> GetResourcePointsForUser(int userId);
    /// <summary>Lists the owner's resources with their category and vehicle or equipment details.</summary>
    Task<List<Resource>> ListOwnedAsync(int ownerId);
    Task<bool> CategoryExistsAsync(int? categoryId);
    Task AddAsync(Resource resource);
    /// <summary>
    /// Finds an owned resource and can include its vehicle or equipment details.
    /// </summary>
    Task<Resource?> FindOwnedAsync(int id, int ownerId, bool includeDetails = false);
    Task SaveChangesAsync();
    /// <summary>
    /// Deletes a resource when it has no assignment history and returns false when an assignment
    /// prevents deletion.
    /// </summary>
    Task<bool> TryDeleteAsync(Resource resource);
    /// <summary>Lists the owner's resources with their category for the public actor resource list.</summary>
    Task<List<Resource>> ListOwnedWithCategoryAsync(int ownerId);
}