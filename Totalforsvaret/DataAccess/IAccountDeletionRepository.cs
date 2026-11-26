using Microsoft.AspNetCore.Identity;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

public interface IAccountDeletionRepository
{
    Task<IdentityResult> DeleteAsync(PrivateActor user);
    Task<IdentityResult> DeleteAsync(PublicActor user);
}