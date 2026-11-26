using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

public interface IProfilePhotoRepository
{
    Task<bool> ExistsAsync(int userId);
    ValueTask<ProfilePhoto?> FindAsync(int userId);
    Task SaveAsync(int userId, byte[] content, string contentType);
}