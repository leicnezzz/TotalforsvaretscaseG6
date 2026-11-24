using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.DataAccess;

public interface INeedRepository
{
    Task<Need?> FindAsync(int id, bool trackChanges = false);
    Task AddAsync(Need need);
    Task DeleteAsync(Need need);
    Task SaveChangesAsync();
}