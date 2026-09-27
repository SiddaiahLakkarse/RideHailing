namespace RideHailing.Domain;

public sealed class Payment
{
    public Payment(Guid rideId, PaymentMethod method, decimal amount, string currency) { Id = Guid.NewGuid(); RideId = rideId; Method = method; Amount = amount; Currency = currency; Status = PaymentStatus.Pending; CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow; }

    public Guid Id { get; }
    public Guid RideId { get; }
    public PaymentMethod Method { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public PaymentStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Mark(PaymentStatus status, string? reference = null) { Status = status; ProviderReference = reference ?? ProviderReference; UpdatedAtUtc = DateTime.UtcNow; }
}
