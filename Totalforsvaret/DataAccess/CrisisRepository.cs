using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.ViewModels;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

public sealed class CrisisRepository(AppDbContext db) : ICrisisRepository
{
    public Task<List<Crisis>> ListOverviewAsync() => db.Crises
        .AsNoTracking()
        .OrderByDescending(crisis =>
            crisis.Severity == "Kritisk" ? 4 :
            crisis.Severity == "Høy" ? 3 :
            crisis.Severity == "Middels" ? 2 :
            crisis.Severity == "Lav" ? 1 : 0)
        .ThenByDescending(crisis => crisis.StartTime)
        .ThenByDescending(crisis => crisis.Id)
        .ToListAsync();

    public Task<Crisis?> FindAsync(int id, bool trackChanges = false)
    {
        var query = trackChanges ? db.Crises.AsTracking() : db.Crises.AsNoTracking();
        return query.SingleOrDefaultAsync(crisis => crisis.Id == id);
    }

    public Task<Crisis?> FindLatestAlertAsync(CancellationToken cancellationToken) =>
        db.Crises.AsNoTracking()
            .Where(crisis => (crisis.Status == CrisisStatus.Active || crisis.Status == CrisisStatus.UnderControl) &&
                (crisis.Severity == "Kritisk" || crisis.Severity == "Høy"))
            .OrderByDescending(crisis => crisis.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id) => db.Crises.AnyAsync(crisis => crisis.Id == id);

    public async Task AddAsync(Crisis crisis)
    {
        db.Crises.Add(crisis);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Crisis crisis)
    {
        var needs = await db.Needs.Where(need => need.CrisisId == crisis.Id).ToListAsync();
        var assignments = await db.ResourceAssignments
            .Where(assignment => assignment.Need.CrisisId == crisis.Id)
            .ToListAsync();

        db.ResourceAssignments.RemoveRange(assignments);
        db.Needs.RemoveRange(needs);
        db.Crises.Remove(crisis);
        await db.SaveChangesAsync();
    }

    public Task CloseIfActiveAsync(int id) => db.Crises
        .Where(crisis => crisis.Id == id && crisis.Status == CrisisStatus.Active)
        .ExecuteUpdateAsync(update => update.SetProperty(crisis => crisis.Status, CrisisStatus.Closed));

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public Task<List<CrisisMapPoint>> ListMapPointsAsync(IReadOnlyCollection<int> ids)
    {
        var query = db.Crises.AsNoTracking()
            .Where(crisis => crisis.Latitude >= -90 && crisis.Latitude <= 90 &&
                             crisis.Longitude >= -180 && crisis.Longitude <= 180);
        if (ids.Count > 0)
            query = query.Where(crisis => ids.Contains(crisis.Id));

        return query.Select(crisis => new CrisisMapPoint(
            crisis.Id, crisis.Name, crisis.Latitude!.Value, crisis.Longitude!.Value)).ToListAsync();
    }

    public Task<List<CrisisOption>> ListOptionsAsync(IReadOnlyCollection<int>? ids)
    {
        var query = db.Crises.AsNoTracking();
        if (ids is { Count: > 0 })
            query = query.Where(crisis => ids.Contains(crisis.Id));

        return query.OrderBy(crisis => crisis.Name)
            .Select(crisis => new CrisisOption(crisis.Id, crisis.Name))
            .ToListAsync();
    }
}