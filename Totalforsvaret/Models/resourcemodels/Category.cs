using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models.resourcemodels
{
    public class Category
    {
        public int CategoryId { get; set; } // Primary key
        [MaxLength(100)]
        public required string Name { get; set; }
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}
