namespace Totalforsvaret.Models;

/// <summary>Carries the request identifier shown on the application error page.</summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
