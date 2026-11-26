using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

public sealed class CrisisMapAnnotationRepository(AppDbContext db) : ICrisisMapAnnotationRepository
{
    public async Task AddAsync(CrisisMapAnnotation annotation)
    {
        db.CrisisMapAnnotations.Add(annotation);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int crisisId, int annotationId)
    {
        var annotation = await db.CrisisMapAnnotations.SingleOrDefaultAsync(
            item => item.Id == annotationId && item.CrisisId == crisisId);
        if (annotation is null)
            return false;

        db.CrisisMapAnnotations.Remove(annotation);
        await db.SaveChangesAsync();
        return true;
    }
}