using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models.resourcemodels
{
    // Representerer en kategori som kan tilordnes ressurser i systemet.
    public class Category
    {
        public int CategoryId { get; set; } 
        [MaxLength(100)]
        public required string Name { get; set; } 
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}
