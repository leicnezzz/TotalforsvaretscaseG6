using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Services;
using static Totalforsvaret.Tests.Helpers.PublicActorTestHelper;

namespace Totalforsvaret.Tests;

public class InboxRepositoryAccessTests
{
    [Test]
    public async Task PublicActorsShareThreadsWhilePrivateActorsOnlyAccessTheirOwn()
    {
        using var db = CreateDb();
        db.Users.Add(new PublicActor { Id = 20, Navn = "Offentlig avsender" });
        await db.SaveChangesAsync();
        AddNeed(db, 1);
        AddResource(db, 1, userId: 10);
        AddAssignment(db, 42, 1, 1, AssignmentStatus.Requested);
        var repository = new ResourceAssignmentRepository(db);
        var inbox = new InboxService(repository, new ResourceAssignmentService(repository));

        var sent = await inbox.SendMessageAsync(20, InboxAudience.Public, 42, "Hei");
        var sharedThread = await inbox.GetInboxAsync(21, InboxAudience.Public, 42);
        var ownerThread = await inbox.GetInboxAsync(10, InboxAudience.Private, 42);
        var otherPrivateInbox = await inbox.GetInboxAsync(11, InboxAudience.Private, 42);
        var denied = await inbox.SendMessageAsync(11, InboxAudience.Private, 42, "Hei");

        Assert.That(sent.Outcome, Is.EqualTo(InboxOutcome.Success));
        Assert.That(sharedThread.SelectedId, Is.EqualTo(42));
        Assert.That(sharedThread.Messages.Count, Is.EqualTo(1));
        Assert.That(ownerThread.Messages.Count, Is.EqualTo(1));
        Assert.That(otherPrivateInbox.Items, Is.Empty);
        Assert.That(otherPrivateInbox.Messages, Is.Empty);
        Assert.That(denied.Outcome, Is.EqualTo(InboxOutcome.NotFound));
    }

    [Test]
    public async Task PrivateInboxDoesNotLoadMessagesForAnInvisibleAssignment()
    {
        var repository = new Mock<IResourceAssignmentRepository>(MockBehavior.Strict);
        repository.Setup(repo => repo.ListForResourceOwnerAsync(10))
            .ReturnsAsync(new List<ResourceAssignment>());
        var inbox = new InboxService(repository.Object, new ResourceAssignmentService(repository.Object));

        var model = await inbox.GetInboxAsync(10, InboxAudience.Private, 42);

        Assert.That(model.SelectedId, Is.Null);
        Assert.That(model.Messages, Is.Empty);
        repository.Verify(repo => repo.ListForResourceOwnerAsync(10), Times.Once);
        repository.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PrivateSenderCannotPostToAnAssignmentTheyDoNotOwn()
    {
        var repository = new Mock<IResourceAssignmentRepository>(MockBehavior.Strict);
        repository.Setup(repo => repo.ExistsForOwnerAsync(42, 10)).ReturnsAsync(false);
        var inbox = new InboxService(repository.Object, new ResourceAssignmentService(repository.Object));

        var result = await inbox.SendMessageAsync(10, InboxAudience.Private, 42, "Hei");

        Assert.That(result.Outcome, Is.EqualTo(InboxOutcome.NotFound));
        repository.Verify(repo => repo.ExistsForOwnerAsync(42, 10), Times.Once);
        repository.VerifyNoOtherCalls();
    }
}
