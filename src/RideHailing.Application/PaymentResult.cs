using RideHailing.Domain;

namespace RideHailing.Application;

public sealed record PaymentResult(PaymentStatus Status, string? ProviderReference, string? FailureReason = null);
