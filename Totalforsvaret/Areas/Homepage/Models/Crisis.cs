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
    public string Area { get; set; } = "";

    [Column("Breddegrad")]
    public double? Latitude { get; set; }

    [Column("Lengdegrad")]
    public double? Longitude { get; set; }
    
    // Informasjon om krisen
    [Column("Alvorlighetsgrad")]
    public string Severity { get; set; } = "";

    [Column("Dato")]
    public DateTime StartTime { get; set; }

    public string Status { get; set; } = "";

    // Ny informasjon som må legges til i databasen
    public string Type { get; set; } = "";

    public int? OffentligAktorId { get; set; }

    public Areas.Offentlig.Models.Offentligaktør? OffentligAktor { get; set; }

    // Behov knyttet til krisen
    public ICollection<Areas.Offentlig.Models.Need> Needs { get; set; }
        = new List<Areas.Offentlig.Models.Need>();
}