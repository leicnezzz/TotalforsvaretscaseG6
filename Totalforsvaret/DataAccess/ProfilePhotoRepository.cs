using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class ProfilePhotoRepository(AppDbContext db) : IProfilePhotoRepository
{
    public Task<bool> ExistsAsync(int userId) =>
        db.ProfilePhotos.AnyAsync(photo => photo.BrukerId == userId);

    public ValueTask<ProfilePhoto?> FindAsync(int userId) =>
        db.ProfilePhotos.FindAsync(userId);


    public async Task SaveAsync(int userId, byte[] content, string contentType)
    {
        var photo = await FindAsync(userId);
        if (photo == null)
        {
            photo = new ProfilePhoto { BrukerId = userId };
            db.ProfilePhotos.Add(photo);
        }

        photo.Content = content;
        photo.ContentType = contentType;
        await db.SaveChangesAsync();
    }
}
