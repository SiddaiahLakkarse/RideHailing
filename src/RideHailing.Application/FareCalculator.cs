using RideHailing.Domain;

namespace RideHailing.Application;

public sealed class FareCalculator
{
    public FareBreakdown Calculate(PricingRule rule, decimal distanceKm, decimal durationMinutes, decimal discount = 0)
    {
        if (distanceKm < 0 || durationMinutes < 0 || discount < 0) throw new DomainException("INVALID_PRICING_INPUT", "Distance, duration, and discount must be non-negative.");
        var distance = distanceKm * rule.PerKmRate; var duration = durationMinutes * rule.PerMinuteRate; var subtotal = rule.BaseFare + distance + duration + rule.BookingFee - discount; var taxes = Math.Max(0, subtotal) * rule.TaxRate; var total = Math.Max(rule.MinimumFare, subtotal + taxes);
        return new FareBreakdown(rule.BaseFare, distance, duration, rule.BookingFee, taxes, discount, decimal.Round(total, 2));
    }
}
