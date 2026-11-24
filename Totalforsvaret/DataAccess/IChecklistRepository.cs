namespace Totalforsvaret.DataAccess;

public interface IChecklistRepository
{
    Task<HashSet<string>> GetCompletedAsync(int userId);
    Task SetStatusAsync(int userId, string itemId, bool isComplete);
}
