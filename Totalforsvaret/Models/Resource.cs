namespace Totalforsvaret.Models
{
    public class Resource
    {
        public int ResourceId { get; set; }
        public string Object { get; set; }
        public int CategoryId { get; set; }
        public bool Available { get; set; }
        public Decimal Latitude { get; set; }
        public Decimal Longitude { get; set; }

    }
}
