using Moq;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.DataAccess;
using Totalforsvaret.Models.Inbox;
using Totalforsvaret.Services;

namespace Totalforsvaret.Tests;

public class InboxRepositoryAccessTests
{
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
