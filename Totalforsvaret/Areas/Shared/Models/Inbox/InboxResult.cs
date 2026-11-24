namespace Totalforsvaret.Models.Inbox;

public enum InboxOutcome
{
    Success,
    Rejected,
    NotFound
}

public sealed record InboxResult(InboxOutcome Outcome, string Message)
{
    public static InboxResult Ok(string message) =>
        new(InboxOutcome.Success, message);

    public static InboxResult Fail(string message) =>
        new(InboxOutcome.Rejected, message);

    public static InboxResult Missing() =>
        new(InboxOutcome.NotFound, "");
}