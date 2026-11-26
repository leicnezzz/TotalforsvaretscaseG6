using Microsoft.AspNetCore.Identity;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

/// <summary>
/// Coordinates cleanup of related data and Identity account deletion in one transaction.
/// </summary>
public interface IAccountDeletionRepository
{
    /// <summary>
    /// Deletes a private account after removing its resources and their assignments.
    /// </summary>
    Task<IdentityResult> DeleteAsync(PrivateActor user);
    /// <summary>
    /// Deletes a public account and retains its crises and needs without their creator link.
    /// </summary>
    Task<IdentityResult> DeleteAsync(PublicActor user);
}