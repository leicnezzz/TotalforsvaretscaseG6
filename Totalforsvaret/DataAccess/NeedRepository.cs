using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class NeedRepository : INeedRepository {
    private readonly AppDbContext _db;

    public NeedRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Need?> FindAsync(int id, bool trackChanges = false)
    {
        var query = trackChanges ? _db.Needs.AsTracking() : _db.Needs.AsNoTracking();
        return query.SingleOrDefaultAsync(need => need.Id == id);
    }

    public async Task AddAsync(Need need)
    {
        _db.Needs.Add(need);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Need need)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var locked = await _db.Database.SqlQuery<int>($"SELECT `Id` FROM `Behov` WHERE `Id` = {need.Id} FOR UPDATE")
            .ToListAsync();
        if (locked.Count == 0)
            return;

        var assignments = await _db.ResourceAssignments
            .Where(assignment => assignment.NeedId == need.Id)
            .ToListAsync();

        _db.ResourceAssignments.RemoveRange(assignments);
        _db.Needs.Remove(need);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task UpdateAsync(Need need)
    {
        if (need.Status != NeedStatus.Resolved)
        {
            await _db.SaveChangesAsync();
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        // Save the need first so concurrent requests cannot validate it as open during cancellation.
        _db.Entry(need).Property(item => item.Status).IsModified = true;
        await _db.SaveChangesAsync();

        var closedAt = DateTime.UtcNow;
        await _db.ResourceAssignments
            .Where(assignment => assignment.NeedId == need.Id &&
                assignment.Status == AssignmentStatus.Requested)
            .ExecuteUpdateAsync(update => update
                .SetProperty(assignment => assignment.Status, AssignmentStatus.Cancelled)
                .SetProperty(assignment => assignment.ClosedAtUtc, closedAt));

        await transaction.CommitAsync();
    }
}