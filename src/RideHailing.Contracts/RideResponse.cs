using RideHailing.Domain;

namespace RideHailing.Contracts;

public sealed record RideResponse(
    Guid Id,
    Guid RiderId,
    RideStatus Status,
    Guid? DriverId,
    VehicleType VehicleType,
    string Pickup,
    string Destination,
    string PickupAddress,
    string DestinationAddress,
    decimal? EstimatedFare,
    string Currency,
    DateTime RequestedAtUtc);
