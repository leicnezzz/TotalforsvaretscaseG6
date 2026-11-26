using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class NeedRepository(AppDbContext db) : INeedRepository
{
    public Task<Need?> FindAsync(int id, bool trackChanges = false)
    {
        var query = trackChanges ? db.Needs.AsTracking() : db.Needs.AsNoTracking();
        return query.SingleOrDefaultAsync(need => need.Id == id);
    }

    public async Task AddAsync(Need need)
    {
        db.Needs.Add(need);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Need need)
    {
        var assignments = await db.ResourceAssignments
            .Where(assignment => assignment.NeedId == need.Id)
            .ToListAsync();

        db.ResourceAssignments.RemoveRange(assignments);
        db.Needs.Remove(need);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Need need)
    {
        if (need.Status != NeedStatus.Resolved)
        {
            await db.SaveChangesAsync();
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        // Save the need first so concurrent requests cannot validate it as open during cancellation.
        db.Entry(need).Property(item => item.Status).IsModified = true;
        await db.SaveChangesAsync();

        var closedAt = DateTime.UtcNow;
        await db.ResourceAssignments
            .Where(assignment => assignment.NeedId == need.Id &&
                assignment.Status == AssignmentStatus.Requested)
            .ExecuteUpdateAsync(update => update
                .SetProperty(assignment => assignment.Status, AssignmentStatus.Cancelled)
                .SetProperty(assignment => assignment.ClosedAtUtc, closedAt));

        await transaction.CommitAsync();
    }
}