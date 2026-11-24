using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

public interface IResourceAssignmentRepository
{
    Task<ResourceAssignment?> FindOwnedAsync(int assignmentId, int ownerId);
    Task<bool> HasOtherActiveAssignmentAsync(int assignmentId, int resourceId);
    Task<List<ResourceAssignment>> ListForResourceOwnerAsync(int ownerId);
    Task<List<ResourceAssignment>> ListForPublicInboxAsync();
    Task<List<InboxMessageViewModel>> ListMessagesAsync(int assignmentId, int viewerId);
    Task<bool> ExistsForOwnerAsync(int assignmentId, int ownerId);
    Task<bool> ExistsAsync(int assignmentId);
    Task AddMessageAsync(ResourceMessage message);
    Task<RequestAssignmentResult> RequestAsync(int[] needIds, int[] resourceIds);
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
