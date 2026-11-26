using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.DataAccess;

public interface INeedRepository
{
    /// <summary>
    /// Finds a need by ID, optionally tracking it for an update, and returns null if it is missing.
    /// </summary>
    Task<Need?> FindAsync(int id, bool trackChanges = false);
    Task AddAsync(Need need);

    /// <summary>
    /// Deletes the need and its resource assignments in one save.
    /// </summary>
    Task DeleteAsync(Need need);
    Task SaveChangesAsync();
}