namespace Vakaros.Vkx.Api.Helpers;

/// <summary>
/// Geodesic calculation helpers.
/// </summary>
public static class GeoHelper
{
    private const double EarthRadiusMeters = 6_371_000.0;

    /// <summary>
    /// Returns the great-circle distance in metres between two WGS-84 coordinates
    /// using the Haversine formula.
    /// </summary>
    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
