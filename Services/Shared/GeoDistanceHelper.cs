namespace HRM.Services.Shared;

// Static Haversine great-circle distance — pure, no DB access. Used by
// GpsCheckinService to check an employee's reported lat/lng against
// registered Att_GeofenceLocation rows.
public static class GeoDistanceHelper
{
    private const double EarthRadiusMeters = 6371000;

    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
