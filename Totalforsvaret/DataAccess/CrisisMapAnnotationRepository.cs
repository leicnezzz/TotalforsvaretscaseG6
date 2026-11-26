using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

public sealed class CrisisMapAnnotationRepository : ICrisisMapAnnotationRepository {
    private readonly AppDbContext _db;

    public CrisisMapAnnotationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(CrisisMapAnnotation annotation)
    {
        _db.CrisisMapAnnotations.Add(annotation);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int crisisId, int annotationId)
    {
        var annotation = await _db.CrisisMapAnnotations.SingleOrDefaultAsync(
            item => item.Id == annotationId && item.CrisisId == crisisId);
        if (annotation is null)
            return false;

        _db.CrisisMapAnnotations.Remove(annotation);
        await _db.SaveChangesAsync();
        return true;
    }
}