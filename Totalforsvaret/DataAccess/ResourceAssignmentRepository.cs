using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class ResourceAssignmentRepository(AppDbContext db) : IResourceAssignmentRepository
{
    public Task<ResourceAssignment?> FindOwnedAsync(int assignmentId, int ownerId) =>
        db.ResourceAssignments
            .Include(assignment => assignment.Resource)
            .Include(assignment => assignment.Need)
            .SingleOrDefaultAsync(assignment =>
                assignment.Id == assignmentId &&
                assignment.Resource.BrukerId == ownerId);

    public Task<bool> HasOtherActiveAssignmentAsync(int assignmentId, int resourceId) =>
        db.ResourceAssignments.AnyAsync(other =>
            other.Id != assignmentId &&
            other.ResourceId == resourceId &&
            (other.Status == AssignmentStatus.Requested ||
             other.Status == AssignmentStatus.Assigned));

    public Task<List<ResourceAssignment>> ListForResourceOwnerAsync(int ownerId) =>
        InboxAssignments()
            .Where(assignment => assignment.Resource.BrukerId == ownerId)
            .ToListAsync();

    public Task<List<ResourceAssignment>> ListForPublicInboxAsync() =>
        InboxAssignments().ToListAsync();

    private IQueryable<ResourceAssignment> InboxAssignments() =>
        db.ResourceAssignments.AsNoTracking()
            .Include(assignment => assignment.Resource)
                .ThenInclude(resource => resource.Category)
            .Include(assignment => assignment.Need)
            .OrderByDescending(assignment => assignment.RequestedAtUtc);

    public Task<List<InboxMessageViewModel>> ListMessagesAsync(int assignmentId, int viewerId) =>
        db.ResourceMessages.AsNoTracking()
            .Where(message => message.AssignmentId == assignmentId)
            .OrderBy(message => message.SentAtUtc)
            .Select(message => new InboxMessageViewModel
            {
                SenderName = message.Sender.Navn,
                Body = message.Body,
                SentAtUtc = message.SentAtUtc,
                IsOwnMessage = message.SenderUserId == viewerId
            })
            .ToListAsync();

    public Task<bool> ExistsForOwnerAsync(int assignmentId, int ownerId) =>
        db.ResourceAssignments.AnyAsync(assignment =>
            assignment.Id == assignmentId && assignment.Resource.BrukerId == ownerId);

    public Task<bool> ExistsAsync(int assignmentId) =>
        db.ResourceAssignments.AnyAsync(assignment => assignment.Id == assignmentId);

    public async Task AddMessageAsync(ResourceMessage message)
    {
        db.ResourceMessages.Add(message);
        await db.SaveChangesAsync();
    }

    public async Task<RequestAssignmentResult> RequestAsync(int[] needIds, int[] resourceIds)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var activeNeeds = await db.Needs.CountAsync(need =>
            needIds.Contains(need.Id) && (need.Status == NeedStatus.Open || need.Status == NeedStatus.InProgress));
        if (activeNeeds != needIds.Length)
            return RequestAssignmentResult.InactiveNeed;

        var availableResources = await db.Resources.CountAsync(resource =>
            resourceIds.Contains(resource.ResourceId) && resource.Available &&
            (resource.TilgjengeligFra == null || resource.TilgjengeligFra <= DateTime.Now));
        if (availableResources != resourceIds.Length)
            return RequestAssignmentResult.UnavailableResource;

        var busy = await db.ResourceAssignments.AnyAsync(assignment =>
            resourceIds.Contains(assignment.ResourceId) &&
            (assignment.Status == AssignmentStatus.Requested || assignment.Status == AssignmentStatus.Assigned));
        if (busy)
            return RequestAssignmentResult.BusyResource;

        var requestedAt = DateTime.UtcNow;
        db.ResourceAssignments.AddRange(resourceIds.Select((id, index) => new ResourceAssignment
        {
            NeedId = needIds.Length == 1 ? needIds[0] : needIds[index],
            ResourceId = id,
            Status = AssignmentStatus.Requested,
            RequestedAtUtc = requestedAt
        }));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return RequestAssignmentResult.Requested;
    }

    public Task<ResourceAssignment?> FindAsync(int assignmentId) =>
        db.ResourceAssignments.FirstOrDefaultAsync(assignment => assignment.Id == assignmentId);

    public Task<bool> HasActiveNeedAsync(int needId) => db.Needs.AnyAsync(need =>
        need.Id == needId && (need.Status == NeedStatus.Open || need.Status == NeedStatus.InProgress));

    public Task<bool> HasAvailableResourceAsync(int resourceId) => db.Resources.AnyAsync(resource =>
        resource.ResourceId == resourceId && resource.Available &&
        (resource.TilgjengeligFra == null || resource.TilgjengeligFra <= DateTime.Now));

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
