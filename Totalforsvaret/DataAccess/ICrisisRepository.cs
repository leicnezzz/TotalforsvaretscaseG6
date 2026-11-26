using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

/// <summary>
/// Provides persistence and read queries for crises.
/// </summary>
public interface ICrisisRepository
{
    /// <summary>
    /// Lists crises by severity, then by most recent start time and ID.
    /// </summary>
    Task<List<Crisis>> ListOverviewAsync();
    /// <summary>
    /// Finds the newest high or critical crisis that is active or under control.
    /// </summary>
    Task<Crisis?> FindLatestAlertAsync(CancellationToken cancellationToken);
    /// <summary>Finds a crisis by ID, optionally tracking it for an update, and returns null if it is missing.</summary>
    Task<Crisis?> FindAsync(int id, bool trackChanges = false);
    Task<bool> ExistsAsync(int id);
    Task AddAsync(Crisis crisis);
    /// <summary>
    /// Deletes a crisis, its needs and their resource assignments in one save.
    /// </summary>
    Task DeleteAsync(Crisis crisis);
    /// <summary>
    /// Changes an active crisis to closed and leaves any other status unchanged.
    /// </summary>
    Task CloseIfActiveAsync(int id);
    Task SaveChangesAsync();
    /// <summary>
    /// Returns valid crisis map points, restricted to the supplied IDs when the selection is
    /// nonempty.
    /// </summary>
    Task<List<CrisisMapPoint>> ListMapPointsAsync(IReadOnlyCollection<int> ids);
    /// <summary>Lists crisis choices by name. A null or empty selection includes all crises.</summary>
    Task<List<CrisisOption>> ListOptionsAsync(IReadOnlyCollection<int>? ids);
}

/// <summary>Carries the identifier and display name of a crisis selectable in a form.</summary>
public record CrisisOption(int Id, string Name);