using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class ProfilePhotoRepository : IProfilePhotoRepository {
    private readonly AppDbContext _db;

    public ProfilePhotoRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsAsync(int userId) =>
        _db.ProfilePhotos.AnyAsync(photo => photo.BrukerId == userId);

    public ValueTask<ProfilePhoto?> FindAsync(int userId) =>
        _db.ProfilePhotos.FindAsync(userId);


    public async Task SaveAsync(int userId, byte[] content, string contentType)
    {
        var photo = await FindAsync(userId);
        if (photo == null)
        {
            photo = new ProfilePhoto { BrukerId = userId };
            _db.ProfilePhotos.Add(photo);
        }

        photo.Content = content;
        photo.ContentType = contentType;
        await _db.SaveChangesAsync();
    }
}
