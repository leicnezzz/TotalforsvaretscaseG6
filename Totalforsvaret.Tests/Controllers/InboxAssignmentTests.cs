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
        return new InboxService(repository, new ResourceAssignmentService(repository));
    }
}
