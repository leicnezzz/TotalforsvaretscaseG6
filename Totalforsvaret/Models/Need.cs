namespace Totalforsvaret.Models;

public class Need
{
    public int Id { get; set; }

    public string Type { get; set; } = "";

    public string Description { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}