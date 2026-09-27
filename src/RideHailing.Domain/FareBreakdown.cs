namespace RideHailing.Domain;

public sealed record FareBreakdown(decimal BaseFare, decimal DistanceFare, decimal DurationFare, decimal BookingFee, decimal Taxes, decimal Discount, decimal Total);
