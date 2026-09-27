using RideHailing.Domain;

namespace RideHailing.Application;

public sealed record CreateRideInput(VehicleType VehicleType, GeoPoint Pickup, string PickupAddress, GeoPoint Destination, string DestinationAddress, PricingRule Pricing, decimal Discount = 0);
