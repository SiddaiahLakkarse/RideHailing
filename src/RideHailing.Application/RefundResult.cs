namespace RideHailing.Application;

public sealed record RefundResult(bool Succeeded, string? ProviderReference);
