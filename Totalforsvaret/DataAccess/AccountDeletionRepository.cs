using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class AccountDeletionRepository(AppDbContext db, UserManager<Bruker> users)
    : IAccountDeletionRepository
{
    public Task<IdentityResult> DeleteAsync(Privataktor user) => DeleteAsync(user, isPublicActor: false);

    public Task<IdentityResult> DeleteAsync(Offentligaktør user) => DeleteAsync(user, isPublicActor: true);

    private async Task<IdentityResult> DeleteAsync(Bruker user, bool isPublicActor)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        if (isPublicActor)
        {
            await db.Crises.Where(crisis => crisis.OffentligAktorId == user.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(crisis => crisis.OffentligAktorId, (int?)null));
            await db.Needs.Where(need => need.OffentligAktorId == user.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(need => need.OffentligAktorId, (int?)null));
        }

        var resources = db.Resources.Where(resource => resource.BrukerId == user.Id);
        await db.ResourceAssignments
            .Where(assignment => resources.Any(resource => resource.ResourceId == assignment.ResourceId))
            .ExecuteDeleteAsync();
        await resources.ExecuteDeleteAsync();

        if (isPublicActor)
        {
            await db.ResourceMessages.Where(message => message.SenderUserId == user.Id)
                .ExecuteDeleteAsync();
        }

        var result = await users.DeleteAsync(user);
        if (result.Succeeded)
            await transaction.CommitAsync();

        return result;
    }
}