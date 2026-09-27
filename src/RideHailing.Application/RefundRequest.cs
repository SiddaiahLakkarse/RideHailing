namespace RideHailing.Application;

public sealed record RefundRequest(Guid PaymentId, decimal Amount, string Currency);
