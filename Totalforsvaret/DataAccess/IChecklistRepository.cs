namespace Totalforsvaret.DataAccess;

/// <summary>
/// Stores checklist completion per user and checklist item.
/// </summary>
public interface IChecklistRepository
{
    Task<HashSet<string>> GetCompletedAsync(int userId);
    /// <summary>
    /// Creates a progress record for the item or updates its completion status.
    /// </summary>
    Task SetStatusAsync(int userId, string itemId, bool isComplete);
}
