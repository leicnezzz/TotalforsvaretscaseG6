using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

/// <summary>
/// Stores map annotations linked to a crisis.
/// </summary>
public interface ICrisisMapAnnotationRepository
{
    Task AddAsync(CrisisMapAnnotation annotation);
    /// <summary>
    /// Deletes an annotation only when both its ID and crisis ID match.
    /// </summary>
    Task<bool> DeleteAsync(int crisisId, int annotationId);
}