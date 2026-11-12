namespace Totalforsvaret.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
// Informasjon som vises på feilsiden. RequestId brukes for å spore feilen i loggene. 