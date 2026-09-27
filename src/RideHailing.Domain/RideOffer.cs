namespace RideHailing.Domain;

public sealed record RideOffer(Guid RideId, Guid DriverId, DateTime ExpiresAtUtc, bool Accepted = false, bool Rejected = false)
{
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}
