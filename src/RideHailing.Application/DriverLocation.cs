using RideHailing.Domain;

namespace RideHailing.Application;

public sealed record DriverLocation(Guid DriverId, GeoPoint Position, double AccuracyMeters, double? Heading, double? SpeedMps, DateTime UpdatedAtUtc);
