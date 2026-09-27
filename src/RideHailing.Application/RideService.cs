using RideHailing.Domain;

namespace RideHailing.Application;

public sealed class RideService(IRideRepository rides, FareCalculator pricing)
{
    public Task<Ride?> GetAsync(Guid rideId, CancellationToken ct) => rides.GetAsync(rideId, ct);
    public async Task<Ride> CreateAsync(Guid riderId, VehicleType type, GeoPoint pickup, string pickupAddress, GeoPoint destination, string destinationAddress, PricingRule rule, CancellationToken ct)
    { var ride = new Ride(riderId, type, pickup, pickupAddress, destination, destinationAddress); var distance = ApproximateDistanceKm(pickup, destination); var fare = pricing.Calculate(rule, (decimal)distance, (decimal)(distance * 2)).Total; ride.SetEstimate((int)(distance * 1000), (int)(distance * 120), fare); ride.TransitionTo(RideStatus.Matching); await rides.AddAsync(ride, ct); await rides.SaveAsync(ct); return ride; }
    public async Task<bool> AssignAsync(Guid rideId, Guid driverId, CancellationToken ct) { var ride = await rides.GetAsync(rideId, ct) ?? throw new DomainException("RIDE_NOT_FOUND", "Ride was not found."); var assigned = ride.TryAssign(driverId); if (assigned) await rides.SaveAsync(ct); return assigned; }
    private static double ApproximateDistanceKm(GeoPoint a, GeoPoint b) { const double earth = 6371; var dLat = (b.Latitude - a.Latitude) * Math.PI / 180; var dLon = (b.Longitude - a.Longitude) * Math.PI / 180; var x = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(a.Latitude * Math.PI / 180) * Math.Cos(b.Latitude * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2); return earth * 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x)); }
}
