using RideHailing.Domain;

namespace RideHailing.Application;

public interface ILocationService
{
    Task<IReadOnlyList<Guid>> FindNearbyDriversAsync(GeoPoint pickup, VehicleType type, double radiusKm, CancellationToken ct);
}
