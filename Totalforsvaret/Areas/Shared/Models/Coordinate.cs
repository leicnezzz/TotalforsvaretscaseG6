namespace Totalforsvaret.Models;

public readonly record struct Coordinate
{
    public const int LatitudeLimit = 90;
    public const int LongitudeLimit = 180;

    private Coordinate(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public decimal Latitude { get; }
    public decimal Longitude { get; }

    public static bool TryCreate(decimal? latitude, decimal? longitude, out Coordinate coordinate)
    {
        if (latitude is >= -LatitudeLimit and <= LatitudeLimit && longitude is >= -LongitudeLimit and <= LongitudeLimit)
        {
            coordinate = new Coordinate(latitude.Value, longitude.Value);
            return true;
        }

        coordinate = default;
        return false;
    }
}
