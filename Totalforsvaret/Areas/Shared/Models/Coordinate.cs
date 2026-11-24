namespace Totalforsvaret.Models;

public readonly record struct Coordinate
{
    private Coordinate(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public decimal Latitude { get; }
    public decimal Longitude { get; }

    public static bool TryCreate(decimal? latitude, decimal? longitude, out Coordinate coordinate)
    {
        if (latitude is >= -90m and <= 90m && longitude is >= -180m and <= 180m)
        {
            coordinate = new Coordinate(latitude.Value, longitude.Value);
            return true;
        }

        coordinate = default;
        return false;
    }
}
