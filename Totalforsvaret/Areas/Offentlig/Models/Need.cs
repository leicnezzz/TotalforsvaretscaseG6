using System.ComponentModel.DataAnnotations.Schema;

namespace Totalforsvaret.Areas.Offentlig.Models;

[Table("Behov")]
public class Need
{
    public int Id { get; set; }

    // Grunnleggende informasjon
    [Column("Tittel")]
    public string Name { get; set; } = "";

    public string Type { get; set; } = "";

    [Column("Beskrivelse")]
    public string Description { get; set; } = "";

    // Prioritet og status
    [Column("Prioritet")]
    public string Priority { get; set; } = "";

    public string Status { get; set; } = "";

    // Kontaktinformasjon
    public string? Kontaktpunkt { get; set; }

    public string? Kontaktperson { get; set; }

    public string? Telefonnummer { get; set; }
    
    public DateTime? Frist { get; set; }
    
    // Lokasjon
    [Column("Breddegrad")]
    public double? Latitude { get; set; }

    [Column("Lengdegrad")]
    public double? Longitude { get; set; }

    // Kobling til krise
    [Column("HendelseId")]
    public int CrisisId { get; set; }

    public Totalforsvaret.Models.Crisis? Crisis { get; set; }

    // Kobling til offentlig aktør
    [Column("OffentligAktørId")]
    public int OffentligAktorId { get; set; }

    public Offentligaktør? OffentligAktor { get; set; }
}