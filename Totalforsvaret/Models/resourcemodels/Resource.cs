using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models.resourcemodels
{
    public class Resource
    {
        
        public int ResourceId { get; set; }
        [MaxLength(100)]
        public required string Object { get; set; }
        public  int CategoryId { get; set; } // Foreign key to Category
        public Category Category { get; set; } = null!;
        public bool Available { get; set; }
        [MaxLength(200)]
        public string Navn { get; set; } = string.Empty;
        [MaxLength(200)]
        public string Kontaktpunkt { get; set; } = string.Empty;
        public DateTime? TilgjengeligFra { get; set; }
        public Decimal Latitude { get; set; }
        public Decimal Longitude { get; set; }

    }
}

// View change line X
