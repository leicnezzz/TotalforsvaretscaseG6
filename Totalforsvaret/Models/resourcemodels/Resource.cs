namespace Totalforsvaret.Models.resourcemodels
{
    public class Resource
    {
        public int ResourceId { get; set; }
        public string Object { get; set; }
        public int CategoryId { get; set; } // Foreign key to Category
        public bool Available { get; set; }
        public Decimal Latitude { get; set; }
        public Decimal Longitude { get; set; }

    }
}
