using System.Collections.Concurrent;
using RideHailing.Domain;

namespace RideHailing.Application;

public sealed class ApplicationStore
{
    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<Guid, DriverProfile> Drivers { get; } = new();
    public ConcurrentDictionary<Guid, Vehicle> Vehicles { get; } = new();
    public ConcurrentDictionary<Guid, Ride> Rides { get; } = new();
    public ConcurrentDictionary<string, RideOffer> Offers { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<Guid, Payment> Payments { get; } = new();
    public ConcurrentDictionary<Guid, Rating> Ratings { get; } = new();
    public ConcurrentDictionary<Guid, DriverLocation> Locations { get; } = new();
    public ConcurrentDictionary<string, Guid> RefreshTokens { get; } = new(StringComparer.Ordinal);
}
