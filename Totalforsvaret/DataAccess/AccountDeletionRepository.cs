using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class AccountDeletionRepository : IAccountDeletionRepository {
    private readonly AppDbContext _db;
    private readonly UserManager<Bruker> _users;

    public AccountDeletionRepository(AppDbContext db, UserManager<Bruker> users)
    {
        _db = db;
        _users = users;
    }

    public Task<IdentityResult> DeleteAsync(PrivateActor user) => DeleteAsync(user, isPublicActor: false);

    public Task<IdentityResult> DeleteAsync(PublicActor user) => DeleteAsync(user, isPublicActor: true);

    private async Task<IdentityResult> DeleteAsync(Bruker user, bool isPublicActor)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        if (isPublicActor)
        {
            await _db.Crises.Where(crisis => crisis.OffentligAktorId == user.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(crisis => crisis.OffentligAktorId, (int?)null));
            await _db.Needs.Where(need => need.OffentligAktorId == user.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(need => need.OffentligAktorId, (int?)null));
        }

        var resources = _db.Resources.Where(resource => resource.BrukerId == user.Id);
        await _db.ResourceAssignments
            .Where(assignment => resources.Any(resource => resource.ResourceId == assignment.ResourceId))
            .ExecuteDeleteAsync();
        await resources.ExecuteDeleteAsync();

        if (isPublicActor)
        {
            await _db.ResourceMessages.Where(message => message.SenderUserId == user.Id)
                .ExecuteDeleteAsync();
        }

        var result = await _users.DeleteAsync(user);
        if (result.Succeeded)
            await transaction.CommitAsync();

        return result;
    }
}