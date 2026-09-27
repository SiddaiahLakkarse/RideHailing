namespace RideHailing.Domain;

public sealed class Rating
{
    public Rating(Guid rideId, Guid raterUserId, Guid ratedUserId, byte score, string? comment)
    {
        if (score is < 1 or > 5) throw new DomainException("INVALID_RATING", "Rating must be between 1 and 5.");
        Id = Guid.NewGuid(); RideId = rideId; RaterUserId = raterUserId; RatedUserId = ratedUserId; Score = score; Comment = comment; CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; }
    public Guid RideId { get; }
    public Guid RaterUserId { get; }
    public Guid RatedUserId { get; }
    public byte Score { get; }
    public string? Comment { get; }
    public DateTime CreatedAtUtc { get; }
}
