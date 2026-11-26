using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

public sealed class CrisisRepository : ICrisisRepository {
    private readonly AppDbContext _db;

    public CrisisRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Crisis>> ListOverviewAsync() => _db.Crises
        .AsNoTracking()
        .OrderByDescending(crisis =>
            crisis.Severity == CrisisSeverity.Critical ? 4 :
            crisis.Severity == CrisisSeverity.High ? 3 :
            crisis.Severity == CrisisSeverity.Medium ? 2 :
            crisis.Severity == CrisisSeverity.Low ? 1 : 0)
        .ThenByDescending(crisis => crisis.StartTime)
        .ThenByDescending(crisis => crisis.Id)
        .ToListAsync();

    public Task<Crisis?> FindAsync(int id, bool trackChanges = false)
    {
        var query = trackChanges ? _db.Crises.AsTracking() : _db.Crises.AsNoTracking();
        return query.SingleOrDefaultAsync(crisis => crisis.Id == id);
    }

    public Task<Crisis?> FindLatestAlertAsync(CancellationToken cancellationToken) =>
        _db.Crises.AsNoTracking()
            .Where(crisis => (crisis.Status == CrisisStatus.Active || crisis.Status == CrisisStatus.UnderControl) &&
                (crisis.Severity == CrisisSeverity.Critical || crisis.Severity == CrisisSeverity.High))
            .OrderByDescending(crisis => crisis.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id) => _db.Crises.AnyAsync(crisis => crisis.Id == id);

    public async Task AddAsync(Crisis crisis)
    {
        _db.Crises.Add(crisis);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Crisis crisis)
    {
        var needs = await _db.Needs.Where(need => need.CrisisId == crisis.Id).ToListAsync();
        var assignments = await _db.ResourceAssignments
            .Where(assignment => assignment.Need.CrisisId == crisis.Id)
            .ToListAsync();

        _db.ResourceAssignments.RemoveRange(assignments);
        _db.Needs.RemoveRange(needs);
        _db.Crises.Remove(crisis);
        await _db.SaveChangesAsync();
    }

    public Task CloseIfActiveAsync(int id) => _db.Crises
        .Where(crisis => crisis.Id == id &&
            (crisis.Status == CrisisStatus.Active || crisis.Status == CrisisStatus.UnderControl))
        .ExecuteUpdateAsync(update => update.SetProperty(crisis => crisis.Status, CrisisStatus.Closed));

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public Task<List<CrisisMapPoint>> ListMapPointsAsync(IReadOnlyCollection<int> ids)
    {
        var query = _db.Crises.AsNoTracking()
            .Where(crisis => crisis.Latitude >= -90 && crisis.Latitude <= 90 &&
                             crisis.Longitude >= -180 && crisis.Longitude <= 180);
        if (ids.Count > 0)
            query = query.Where(crisis => ids.Contains(crisis.Id));

        return query.Select(crisis => new CrisisMapPoint(
            crisis.Id, crisis.Name, (double)crisis.Latitude!.Value, (double)crisis.Longitude!.Value)).ToListAsync();
    }

    public Task<List<CrisisOption>> ListOptionsAsync(IReadOnlyCollection<int>? ids)
    {
        var query = _db.Crises.AsNoTracking();
        if (ids is { Count: > 0 })
            query = query.Where(crisis => ids.Contains(crisis.Id));

        return query.OrderBy(crisis => crisis.Name)
            .Select(crisis => new CrisisOption(crisis.Id, crisis.Name))
            .ToListAsync();
    }
}