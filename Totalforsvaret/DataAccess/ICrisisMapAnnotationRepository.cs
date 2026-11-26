using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

public interface ICrisisMapAnnotationRepository
{
    Task AddAsync(CrisisMapAnnotation annotation);
    Task<bool> DeleteAsync(int crisisId, int annotationId);
}