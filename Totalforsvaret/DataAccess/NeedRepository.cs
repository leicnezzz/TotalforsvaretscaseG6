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

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}