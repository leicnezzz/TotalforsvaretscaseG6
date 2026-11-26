using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Totalforsvaret.Areas.Offentlig.Models;

/// <summary>Represents a resource requirement registered by a public actor, optionally linked to a crisis.</summary>
[Table("Behov")]
public class Need
{
    public int Id { get; set; }

    [Column("Tittel")]
    public string Name { get; set; } = "";

    public NeedType Type { get; set; }

    [Column("Beskrivelse")]
    public string Description { get; set; } = "";

    [Column("Prioritet")]
    public NeedPriority Priority { get; set; }

    public NeedStatus Status { get; set; }

    public string? Kontaktpunkt { get; set; }

    public string? Kontaktperson { get; set; }

    public string? Telefonnummer { get; set; }
    
    [Column("Breddegrad")]
    [Precision(9, 6)]
    public decimal? Latitude { get; set; }

    [Column("Lengdegrad")]
    [Precision(9, 6)]
    public decimal? Longitude { get; set; }

    [Column("HendelseId")]
    public int? CrisisId { get; set; }

    public Totalforsvaret.Models.Crisis? Crisis { get; set; }

    [Column("OffentligAktørId")]
    public int? OffentligAktorId { get; set; }

    public PublicActor? OffentligAktor { get; set; }
}