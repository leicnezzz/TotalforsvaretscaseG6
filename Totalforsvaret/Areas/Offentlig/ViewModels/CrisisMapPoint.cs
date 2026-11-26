namespace Totalforsvaret.Areas.Offentlig.ViewModels;

/// <summary>Projects a crisis identifier, name and coordinates for the need registration map.</summary>
public record CrisisMapPoint(int Id, string Name, double Latitude, double Longitude);
