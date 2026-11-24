using Totalforsvaret.Models.Inbox;

namespace Totalforsvaret.Services;

public interface IInboxService
{
    Task<InboxViewModel> GetInboxAsync(
        int userId,
        InboxAudience audience,
        int? selectedId);

    Task<InboxResult> SendMessageAsync(
        int userId,
        InboxAudience audience,
        int assignmentId,
        string? body);

    Task<InboxResult> AcceptAsync(int userId, int assignmentId);
    Task<InboxResult> DeclineAsync(int userId, int assignmentId);
    Task<InboxResult> ReleaseAsync(int userId, int assignmentId);
}