namespace RideHailing.Application;

public sealed record PaymentRequest(Guid RideId, decimal Amount, string Currency, string IdempotencyKey);
