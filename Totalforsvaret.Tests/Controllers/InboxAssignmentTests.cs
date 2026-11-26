using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Services;
using static Totalforsvaret.Tests.Helpers.PublicActorTestHelper;

namespace Totalforsvaret.Tests;

public class InboxAssignmentTests
{
    [TestCase(AssignmentStatus.Requested, NeedStatus.Open, true, InboxOutcome.Success)]
    [TestCase(AssignmentStatus.Assigned, NeedStatus.Open, true, InboxOutcome.Rejected)]
    [TestCase(AssignmentStatus.Requested, NeedStatus.Resolved, true, InboxOutcome.Rejected)]
    [TestCase(AssignmentStatus.Requested, NeedStatus.Open, false, InboxOutcome.Rejected)]
    public async Task AcceptChecksAssignmentNeedAndResource(
        AssignmentStatus status, NeedStatus needStatus, bool available, InboxOutcome expected)
    {
        using var db = CreateDb();
        AddNeed(db, 1, needStatus);
        AddResource(db, 1, available: available);
        AddAssignment(db, 1, 1, 1, status);

        var result = await CreateInbox(db).AcceptAsync(1, 1);

        Assert.That(result.Outcome, Is.EqualTo(expected));
        Assert.That(db.ResourceAssignments.Single().Status,
            Is.EqualTo(expected == InboxOutcome.Success ? AssignmentStatus.Assigned : status));
    }

    [Test]
    public async Task AcceptReturnsNotFoundForMissingAssignment()
    {
        using var db = CreateDb();

        var result = await CreateInbox(db).AcceptAsync(1, 99);

        Assert.That(result.Outcome, Is.EqualTo(InboxOutcome.NotFound));
    }

    [TestCase(AssignmentStatus.Requested, InboxOutcome.Success, AssignmentStatus.Rejected)]
    [TestCase(AssignmentStatus.Assigned, InboxOutcome.Rejected, AssignmentStatus.Assigned)]
    public async Task DeclineOnlyChangesRequestedAssignments(
        AssignmentStatus status, InboxOutcome expected, AssignmentStatus finalStatus)
    {
        using var db = CreateDb();
        AddNeed(db, 1);
        AddResource(db, 1);
        AddAssignment(db, 1, 1, 1, status);

        var result = await CreateInbox(db).DeclineAsync(1, 1);

        Assert.That(result.Outcome, Is.EqualTo(expected));
        Assert.That(db.ResourceAssignments.Single().Status, Is.EqualTo(finalStatus));
    }

    private static InboxService CreateInbox(AppDbContext db)
    {
        var repository = new ResourceAssignmentRepository(db);
        var assignmentStore = new Mock<IResourceAssignmentRepository>(MockBehavior.Strict);
        assignmentStore
            .Setup(store => store.WithResourceLockAsync(
                It.IsAny<ResourceAssignment>(),
                It.IsAny<Func<ResourceAssignment?, Task<AssignmentActionResult>>>()))
            .Returns((ResourceAssignment assignment, Func<ResourceAssignment?, Task<AssignmentActionResult>> action) =>
                action(assignment));
        assignmentStore
            .Setup(store => store.HasOtherActiveAssignmentAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int assignmentId, int resourceId) =>
                repository.HasOtherActiveAssignmentAsync(assignmentId, resourceId));
        assignmentStore.Setup(store => store.SaveChangesAsync()).Returns(() => repository.SaveChangesAsync());

        return new InboxService(repository, new ResourceAssignmentService(assignmentStore.Object));
    }
}
