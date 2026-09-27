namespace RideHailing.Domain;

public sealed class Ride
{
    private static readonly IReadOnlyDictionary<RideStatus, RideStatus[]> Allowed = new Dictionary<RideStatus, RideStatus[]>
    {
        [RideStatus.Requested] = [RideStatus.Matching, RideStatus.Cancelled],
        [RideStatus.Matching] = [RideStatus.DriverAssigned, RideStatus.NoDriverFound, RideStatus.Cancelled],
        [RideStatus.DriverAssigned] = [RideStatus.DriverArriving, RideStatus.Cancelled],
        [RideStatus.DriverArriving] = [RideStatus.DriverWaiting, RideStatus.Cancelled],
        [RideStatus.DriverWaiting] = [RideStatus.TripStarted, RideStatus.Cancelled],
        [RideStatus.TripStarted] = [RideStatus.TripCompleted],
        [RideStatus.TripCompleted] = [RideStatus.PaymentPending],
        [RideStatus.PaymentPending] = [RideStatus.Completed]
    };

    private Ride() { }

    public Ride(Guid riderId, VehicleType vehicleType, GeoPoint pickup, string pickupAddress, GeoPoint destination, string destinationAddress, string currency = "USD")
    {
        if (!pickup.IsValid || !destination.IsValid) throw new DomainException("INVALID_COORDINATES", "Coordinates are invalid.");
        Id = Guid.NewGuid(); RiderId = riderId; VehicleType = vehicleType; Pickup = pickup; PickupAddress = pickupAddress; Destination = destination; DestinationAddress = destinationAddress; Currency = currency; Status = RideStatus.Requested; RequestedAtUtc = DateTime.UtcNow; UpdatedAtUtc = RequestedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RiderId { get; private set; }
    public Guid? DriverId { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public RideStatus Status { get; private set; }
    public GeoPoint Pickup { get; private set; }
    public string PickupAddress { get; private set; } = "";
    public GeoPoint Destination { get; private set; }
    public string DestinationAddress { get; private set; } = "";
    public int? EstimatedDistanceM { get; private set; }
    public int? EstimatedDurationSec { get; private set; }
    public decimal? EstimatedFare { get; private set; }
    public decimal? FinalFare { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void SetEstimate(int distanceM, int durationSec, decimal fare)
    {
        if (distanceM < 0 || durationSec < 0 || fare < 0) throw new DomainException("INVALID_ESTIMATE", "Estimate values cannot be negative.");
        EstimatedDistanceM = distanceM; EstimatedDurationSec = durationSec; EstimatedFare = fare; UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool TryAssign(Guid driverId)
    {
        if (Status != RideStatus.Matching || DriverId.HasValue) return false;
        DriverId = driverId; AssignedAtUtc = DateTime.UtcNow; TransitionTo(RideStatus.DriverAssigned); return true;
    }

    public void TransitionTo(RideStatus next, Guid? changedBy = null, string? reason = null)
    {
        if (!Allowed.TryGetValue(Status, out var targets) || !targets.Contains(next)) throw new DomainException("INVALID_RIDE_TRANSITION", $"Cannot transition ride from {Status} to {next}.");
        var old = Status; Status = next; if (next == RideStatus.TripStarted) StartedAtUtc = DateTime.UtcNow; if (next == RideStatus.TripCompleted) CompletedAtUtc = DateTime.UtcNow; if (next == RideStatus.Cancelled) CancelledAtUtc = DateTime.UtcNow; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetFinalFare(decimal fare)
    {
        if (fare < 0) throw new DomainException("INVALID_FARE", "Fare cannot be negative.");
        FinalFare = fare; UpdatedAtUtc = DateTime.UtcNow;
    }
}
