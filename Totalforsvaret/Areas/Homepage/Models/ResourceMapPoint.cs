namespace Totalforsvaret.Models;

// Bare opplysningene kartet trenger. for å vise punkter på kartet på hoved siden.
public sealed record ResourceMapPoint(
    int Id,
    string Navn,
    decimal Latitude,
    decimal Longitude);