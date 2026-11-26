using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class ChecklistRepository(AppDbContext db) : IChecklistRepository
{
    public Task<HashSet<string>> GetCompletedAsync(int userId) =>
        db.EmergencyChecklistStatuses
            .Where(status => status.BrukerId == userId && status.IsComplete)
            .Select(status => status.ItemId)
            .ToHashSetAsync();

    public async Task SetStatusAsync(int userId, string itemId, bool isComplete)
    {
        var status = await db.EmergencyChecklistStatuses.FindAsync(userId, itemId);
        if (status == null)
        {
            status = new EmergencyChecklistStatus
            {
                BrukerId = userId,
                ItemId = itemId
            };

            db.EmergencyChecklistStatuses.Add(status);
        }

        status.IsComplete = isComplete;
        await db.SaveChangesAsync();
    }
}
