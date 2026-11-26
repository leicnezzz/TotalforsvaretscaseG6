using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

public interface ICrisisRepository
{
    Task<List<Crisis>> ListOverviewAsync();
    Task<Crisis?> FindLatestAlertAsync(CancellationToken cancellationToken);
    Task<Crisis?> FindAsync(int id, bool trackChanges = false);
    Task<bool> ExistsAsync(int id);
    Task AddAsync(Crisis crisis);
    Task DeleteAsync(Crisis crisis);
    Task CloseIfActiveAsync(int id);
    Task SaveChangesAsync();
    Task<List<CrisisMapPoint>> ListMapPointsAsync(IReadOnlyCollection<int> ids);
    Task<List<CrisisOption>> ListOptionsAsync(IReadOnlyCollection<int>? ids);
}

public record CrisisOption(int Id, string Name);