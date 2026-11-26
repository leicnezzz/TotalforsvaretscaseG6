using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models.resourcemodels
{
    /// <summary>Groups registered resources by category for registration and matching.</summary>
    public class Category
    {
        public int CategoryId { get; set; } 
        [MaxLength(100)]
        public required string Name { get; set; } 
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}
