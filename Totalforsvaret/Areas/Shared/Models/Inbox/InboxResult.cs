namespace Totalforsvaret.Models.Inbox;

/// <summary>
/// Distinguishes successful inbox actions, rejected actions and assignments unavailable to the caller.
/// </summary>
public enum InboxOutcome
{
    Success,
    Rejected,
    NotFound
}

/// <summary>Carries an inbox action's outcome and the message to show the caller.</summary>
public sealed record InboxResult(InboxOutcome Outcome, string Message)
{
    public static InboxResult Ok(string message) =>
        new(InboxOutcome.Success, message);

    public static InboxResult Fail(string message) =>
        new(InboxOutcome.Rejected, message);

    public static InboxResult Missing() =>
        new(InboxOutcome.NotFound, "");
}