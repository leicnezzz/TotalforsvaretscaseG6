using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

public interface IResourceAssignmentRepository
{
    /// <summary>
    /// Loads an assignment and its need and resource only if the specified user owns the
    /// resource.
    /// </summary>
    Task<ResourceAssignment?> FindOwnedAsync(int assignmentId, int ownerId);

    /// <summary>
    /// Checks whether another requested or assigned match uses the resource.
    /// </summary>
    Task<bool> HasOtherActiveAssignmentAsync(int assignmentId, int resourceId);

    /// <summary>
    /// Holds the resource row lock until the action completes and its transaction commits.
    /// Passes null to the action if the assignment or resource no longer exists.
    /// </summary>
    Task<TResult> WithResourceLockAsync<TResult>(
        ResourceAssignment assignment,
        Func<ResourceAssignment?, Task<TResult>> action);

    /// <summary>
    /// Lists threads for resources owned by the specified user, newest first.
    /// </summary>
    Task<List<ResourceAssignment>> ListForResourceOwnerAsync(int ownerId);

    /// <summary>
    /// Lists threads visible in the public inbox, newest first.
    /// </summary>
    Task<List<ResourceAssignment>> ListForPublicInboxAsync();

    /// <summary>
    /// Lists a thread's messages in order and marks those sent by the viewer.
    /// </summary>
    Task<List<InboxMessageViewModel>> ListMessagesAsync(int assignmentId, int viewerId);

    Task<bool> ExistsForOwnerAsync(int assignmentId, int ownerId);

    Task<bool> ExistsAsync(int assignmentId);
    Task AddMessageAsync(ResourceMessage message);

    /// <summary>
    /// Validates selected needs and resources and saves requests in one serializable transaction.
    /// </summary>
    Task<RequestAssignmentResult> RequestAsync(int[] needIds, int[] resourceIds);

    /// <summary>
    /// Loads a tracked assignment by ID without filtering by owner or loading its related need and resource.
    /// </summary>
    Task<ResourceAssignment?> FindAsync(int assignmentId);
    Task SaveChangesAsync();
}

public enum RequestAssignmentResult
{
    Requested,
    InactiveNeed,
    UnavailableResource,
    BusyResource
}
