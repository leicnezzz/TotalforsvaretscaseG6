using Totalforsvaret.Models.Inbox;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.DataAccess;

internal sealed class ResourceAssignmentRepository : IResourceAssignmentRepository {
    private readonly AppDbContext _db;

    public ResourceAssignmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<ResourceAssignment?> FindOwnedAsync(int assignmentId, int ownerId) =>
        _db.ResourceAssignments
            .Include(assignment => assignment.Resource)
            .Include(assignment => assignment.Need)
            .SingleOrDefaultAsync(assignment =>
                assignment.Id == assignmentId &&
                assignment.Resource.BrukerId == ownerId);

    public Task<bool> HasOtherActiveAssignmentAsync(int assignmentId, int resourceId) =>
        _db.ResourceAssignments.WhereResourceIsBusy().AnyAsync(other =>
            other.Id != assignmentId && other.ResourceId == resourceId);

    public async Task<TResult> WithResourceLockAsync<TResult>(
        ResourceAssignment assignment,
        Func<ResourceAssignment?, Task<TResult>> action)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted);

        var lockedResources = await _db.Database.SqlQuery<int>($"""
            SELECT `ResourceId`
            FROM `Resources`
            WHERE `ResourceId` = {assignment.ResourceId}
            FOR UPDATE
            """).ToListAsync();

        if (lockedResources.Count == 0)
            return await action(null);

        var entry = _db.Entry(assignment);
        await entry.ReloadAsync();

        if (entry.State == EntityState.Detached)
            return await action(null);

        await _db.Entry(assignment.Resource).ReloadAsync();
        await _db.Entry(assignment.Need).ReloadAsync();

        var result = await action(assignment);
        await transaction.CommitAsync();
        return result;
    }

    public Task<List<ResourceAssignment>> ListForResourceOwnerAsync(int ownerId) =>
        InboxAssignments()
            .Where(assignment => assignment.Resource.BrukerId == ownerId)
            .ToListAsync();

    public Task<List<ResourceAssignment>> ListForPublicInboxAsync() =>
        InboxAssignments().ToListAsync();

    private IQueryable<ResourceAssignment> InboxAssignments() =>
        _db.ResourceAssignments.AsNoTracking()
            .Include(assignment => assignment.Resource)
                .ThenInclude(resource => resource.Category)
            .Include(assignment => assignment.Need)
            .OrderByDescending(assignment => assignment.RequestedAtUtc);

    public Task<List<InboxMessageViewModel>> ListMessagesAsync(int assignmentId, int viewerId) =>
        _db.ResourceMessages.AsNoTracking()
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
        _db.ResourceAssignments.AnyAsync(assignment =>
            assignment.Id == assignmentId && assignment.Resource.BrukerId == ownerId);

    public Task<bool> ExistsAsync(int assignmentId) =>
        _db.ResourceAssignments.AnyAsync(assignment => assignment.Id == assignmentId);

    public async Task AddMessageAsync(ResourceMessage message)
    {
        _db.ResourceMessages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task<RequestAssignmentResult> RequestAsync(int[] needIds, int[] resourceIds)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var activeNeeds = await _db.Needs.CountAsync(need =>
            Enumerable.Contains(needIds, need.Id) && StatusFilters.ActiveNeedStatuses.Contains(need.Status));
        if (activeNeeds != needIds.Length)
            return RequestAssignmentResult.InactiveNeed;

        var now = ResourceAvailability.Now;
        var availableResources = await _db.Resources.CountAsync(resource =>
            Enumerable.Contains(resourceIds, resource.ResourceId) && resource.Available &&
            (resource.TilgjengeligFra == null || resource.TilgjengeligFra <= now));
        if (availableResources != resourceIds.Length)
            return RequestAssignmentResult.UnavailableResource;

        var busy = await _db.ResourceAssignments.WhereResourceIsBusy()
            .AnyAsync(assignment => Enumerable.Contains(resourceIds, assignment.ResourceId));
        if (busy)
            return RequestAssignmentResult.BusyResource;

        var requestedAt = DateTime.UtcNow;
        _db.ResourceAssignments.AddRange(resourceIds.Select((id, index) => new ResourceAssignment
        {
            NeedId = needIds.Length == 1 ? needIds[0] : needIds[index],
            ResourceId = id,
            Status = AssignmentStatus.Requested,
            RequestedAtUtc = requestedAt
        }));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return RequestAssignmentResult.Requested;
    }

    public Task<ResourceAssignment?> FindAsync(int assignmentId) =>
        _db.ResourceAssignments.FirstOrDefaultAsync(assignment => assignment.Id == assignmentId);

    public async Task<bool> TrySaveStatusChangeAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
