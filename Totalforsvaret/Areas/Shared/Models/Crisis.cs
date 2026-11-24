using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models;

[Table("Kriser")]
public class Crisis
{
    public int Id { get; set; }

    // Grunnleggende informasjon
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
    
    // Informasjon om krisen
    [Column("Alvorlighetsgrad")]
    public CrisisSeverity Severity { get; set; }

    [Column("Dato")]
    public DateTime StartTime { get; set; }

    public CrisisStatus Status { get; set; }

    // Ny informasjon som må legges til i databasen
    public CrisisType Type { get; set; }

    public int? OffentligAktorId { get; set; }

    public Areas.Offentlig.Models.PublicActor? OffentligAktor { get; set; }

    // Behov knyttet til krisen
    public ICollection<Areas.Offentlig.Models.Need> Needs { get; set; }
        = new List<Areas.Offentlig.Models.Need>();
}