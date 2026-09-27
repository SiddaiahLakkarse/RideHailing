using RideHailing.Domain;

namespace RideHailing.Contracts;

public sealed record CreateRideRequest(Guid RiderId, VehicleType VehicleType, GeoPoint Pickup, string PickupAddress, GeoPoint Destination, string DestinationAddress);
