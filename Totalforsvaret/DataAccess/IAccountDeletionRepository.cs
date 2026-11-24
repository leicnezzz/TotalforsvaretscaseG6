using Microsoft.AspNetCore.Identity;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

public interface IAccountDeletionRepository
{
    Task<IdentityResult> DeleteAsync(Privataktor user);
    Task<IdentityResult> DeleteAsync(Offentligaktør user);
}