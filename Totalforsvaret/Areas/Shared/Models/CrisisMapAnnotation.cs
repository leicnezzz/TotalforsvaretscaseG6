using System.ComponentModel.DataAnnotations;
namespace Totalforsvaret.Models;

/// <summary>
/// Stores a crisis map marking, such as a risk zone or road obstruction, with its coordinates.
/// </summary>
public class CrisisMapAnnotation
{
    public int Id { get; set; }
    public int CrisisId { get; set; }
    public Crisis Crisis { get; set; } = null!;
    [MaxLength(20)] public string Kind { get; set; } = "";
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(1500)] public string Description { get; set; } = "";
    [MaxLength(100)] public string Impact { get; set; } = "";
    [MaxLength(10)] public string Risk { get; set; } = "";
    public string CoordinatesJson { get; set; } = "[]";
}
