namespace RideHailing.Domain;

public sealed record PricingRule(decimal BaseFare, decimal PerKmRate, decimal PerMinuteRate, decimal MinimumFare, decimal BookingFee, decimal TaxRate = 0);
