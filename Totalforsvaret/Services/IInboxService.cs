using Totalforsvaret.Models.Inbox;

namespace Totalforsvaret.Services;

/// <summary>
/// Provides inbox operations for a signed-in actor. The inbox audience controls thread visibility and messaging.
/// </summary>
public interface IInboxService
{
    /// <summary>Loads visible threads and falls back to the newest visible thread if the requested thread is unavailable.</summary>
    Task<InboxViewModel> GetInboxAsync(
        int userId,
        InboxAudience audience,
        int? selectedId);

    /// <summary>Sends a message about an owned resource for private actors or about any existing assignment for public actors.</summary>
    Task<InboxResult> SendMessageAsync(
        int userId,
        InboxAudience audience,
        int assignmentId,
        string? body);

    /// <summary>Accepts a pending request for an owned resource if the need is active and the resource is available.</summary>
    Task<InboxResult> AcceptAsync(int userId, int assignmentId);
    /// <summary>Rejects a pending request for a resource owned by the caller.</summary>
    Task<InboxResult> DeclineAsync(int userId, int assignmentId);
    /// <summary>Releases an assigned resource owned by the caller while retaining the assignment history.</summary>
    Task<InboxResult> ReleaseAsync(int userId, int assignmentId);
}