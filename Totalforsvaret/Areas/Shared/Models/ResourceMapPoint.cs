namespace Totalforsvaret.Models;

/// <summary>Projects an owned resource's identifier, name and coordinates for the private dashboard map.</summary>
public sealed record ResourceMapPoint(
    int Id,
    string Navn,
    decimal Latitude,
    decimal Longitude);