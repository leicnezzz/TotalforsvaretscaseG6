using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models;

/// <summary>Represents an incident with a location, severity and status that can group related needs.</summary>
[Table("Kriser")]
public class Crisis
{
    public int Id { get; set; }

    [Column("Navn")]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("Beskrivelse")]
    public string Description { get; set; } = "";

    [Column("Sted")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Area { get; set; } = "";

    [Column("Breddegrad")]
    [Precision(9, 6)]
    public decimal? Latitude { get; set; }

    [Column("Lengdegrad")]
    [Precision(9, 6)]
    public decimal? Longitude { get; set; }
    
    [Column("Alvorlighetsgrad")]
    public CrisisSeverity Severity { get; set; }

    [Column("Dato")]
    public DateTime StartTime { get; set; }

    public CrisisStatus Status { get; set; }

    public CrisisType Type { get; set; }

    public int? OffentligAktorId { get; set; }

    public Areas.Offentlig.Models.PublicActor? OffentligAktor { get; set; }

    public ICollection<Areas.Offentlig.Models.Need> Needs { get; set; }
        = new List<Areas.Offentlig.Models.Need>();
}