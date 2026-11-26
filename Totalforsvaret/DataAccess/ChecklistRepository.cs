using Microsoft.EntityFrameworkCore;

namespace Totalforsvaret.DataAccess;

internal sealed class ChecklistRepository : IChecklistRepository {
    private readonly AppDbContext _db;

    public ChecklistRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<HashSet<string>> GetCompletedAsync(int userId) =>
        _db.EmergencyChecklistStatuses
            .Where(status => status.BrukerId == userId && status.IsComplete)
            .Select(status => status.ItemId)
            .ToHashSetAsync();

    public Task SetStatusAsync(int userId, string itemId, bool isComplete) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO `EgenberedskapChecklistStatuses` (`BrukerId`, `ItemId`, `IsComplete`)
            VALUES ({userId}, {itemId}, {isComplete})
            ON DUPLICATE KEY UPDATE `IsComplete` = VALUES(`IsComplete`)
            """);
}
