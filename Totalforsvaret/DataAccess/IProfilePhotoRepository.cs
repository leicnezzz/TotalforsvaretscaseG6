using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

/// <summary>
/// Stores one profile photo per user.
/// </summary>
public interface IProfilePhotoRepository
{
    Task<bool> ExistsAsync(int userId);

    ValueTask<ProfilePhoto?> FindAsync(int userId);

    /// <summary>
    /// Adds a photo or replaces the user's existing photo.
    /// </summary>
    Task SaveAsync(int userId, byte[] content, string contentType);
}