using Microsoft.AspNetCore.Mvc;
using RideHailing.Application;
using RideHailing.Contracts;
using RideHailing.Domain;

namespace RideHailing.Api.Controllers;

[ApiController, Route("api/v1/auth")]
public sealed class AuthController(AuthenticationService auth) : ControllerBase
{
    [HttpPost("register")] public Task<AuthResult> Register(RegisterRequest request, CancellationToken ct) => auth.RegisterAsync(request, ct);
    [HttpPost("login")] public Task<AuthResult> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);
    [HttpPost("refresh")] public Task<AuthResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct) => auth.RefreshAsync(request.RefreshToken, ct);
    [HttpPost("logout")] public IActionResult Logout([FromBody] RefreshRequest request) { auth.Logout(request.RefreshToken); return NoContent(); }
}
public sealed record RefreshRequest(string RefreshToken);

[ApiController, Route("api/v1/rides")]
public sealed class RidesController(RideWorkflowService workflow, ApplicationStore store) : ControllerBase
{
    [HttpPost("estimate")] public ActionResult<FareBreakdown> Estimate(EstimateRequest request) => Ok(new FareCalculator().Calculate(request.Pricing, request.DistanceKm, request.DurationMinutes, request.Discount));
    [HttpPost] public ActionResult<RideResponse> Create(CreateRideRequest request) => Ok(ToResponse(workflow.Create(request.RiderId, new(request.VehicleType, request.Pickup, request.PickupAddress, request.Destination, request.DestinationAddress, new(3, 1.2m, .25m, 5, 1, .05m)), HttpContext.RequestAborted)));
    [HttpGet("history")] public ActionResult<IReadOnlyList<RideResponse>> History([FromQuery] Guid userId) => Ok(store.Rides.Values.Where(x => x.RiderId == userId && x.Status is RideStatus.Completed or RideStatus.Cancelled).OrderByDescending(x => x.RequestedAtUtc).Select(ToResponse).ToArray());
    [HttpGet("active")] public ActionResult<RideResponse> Active([FromQuery] Guid userId)
    {
        var ride = store.Rides.Values.FirstOrDefault(x => x.RiderId == userId && x.Status is not (RideStatus.Completed or RideStatus.Cancelled));
        return Ok(ride is null ? null : ToResponse(ride));
    }
    [HttpGet("{rideId:guid}")] public ActionResult<RideResponse> Get(Guid rideId) => store.Rides.TryGetValue(rideId, out var ride) ? Ok(ToResponse(ride)) : NotFound(new ErrorResponse("RIDE_NOT_FOUND", "Ride was not found.", HttpContext.TraceIdentifier));
    [HttpPost("{rideId:guid}/cancel")] public ActionResult<RideResponse> Cancel(Guid rideId, ActorRequest request) => Ok(ToResponse(workflow.Cancel(rideId, request.UserId, request.Reason ?? "cancelled")));
    [HttpPost("{rideId:guid}/accept")] public ActionResult<RideResponse> Accept(Guid rideId, ActorRequest request) => Ok(ToResponse(workflow.Accept(rideId, request.UserId)));
    [HttpPost("{rideId:guid}/reject")] public ActionResult<RideResponse> Reject(Guid rideId, ActorRequest request) => Ok(ToResponse(workflow.Reject(rideId, request.UserId)));
    [HttpPost("{rideId:guid}/arrived")] public ActionResult<RideResponse> Arrived(Guid rideId, ActorRequest request) => Advance(rideId, request.UserId, RideStatus.DriverArriving);
    [HttpPost("{rideId:guid}/start")] public ActionResult<RideResponse> Start(Guid rideId, ActorRequest request) => Advance(rideId, request.UserId, RideStatus.TripStarted);
    [HttpPost("{rideId:guid}/complete")] public ActionResult<RideResponse> Complete(Guid rideId, ActorRequest request) => Advance(rideId, request.UserId, RideStatus.TripCompleted);
    [HttpPost("{rideId:guid}/payment")] public ActionResult<Payment> Pay(Guid rideId, PaymentRequestDto request) => Ok(workflow.Pay(rideId, request.Method));
    [HttpPost("{rideId:guid}/rating")] public ActionResult<Rating> Rate(Guid rideId, RatingRequest request) => Ok(workflow.Rate(rideId, request.UserId, request.Score, request.Comment));
    [HttpPost("{rideId:guid}/location")] public IActionResult Location(Guid rideId, LocationRequest request) { workflow.UpdateLocation(request.DriverId, rideId, new(request.Latitude, request.Longitude), request.AccuracyMeters, request.Heading, request.SpeedMps); return Accepted(); }
    private ActionResult<RideResponse> Advance(Guid rideId, Guid driverId, RideStatus status) => Ok(ToResponse(workflow.Advance(rideId, driverId, status)));
    internal static RideResponse ToResponse(Ride ride) => new(ride.Id, ride.RiderId, ride.Status, ride.DriverId, ride.VehicleType, ride.PickupAddress, ride.DestinationAddress, ride.PickupAddress, ride.DestinationAddress, ride.EstimatedFare, ride.Currency, ride.RequestedAtUtc);
}
public sealed record EstimateRequest(PricingRule Pricing, decimal DistanceKm, decimal DurationMinutes, decimal Discount = 0);
public sealed record ActorRequest(Guid UserId, string? Reason = null);
public sealed record PaymentRequestDto(PaymentMethod Method);
public sealed record RatingRequest(Guid UserId, byte Score, string? Comment);
public sealed record LocationRequest(Guid DriverId, double Latitude, double Longitude, double AccuracyMeters, double? Heading, double? SpeedMps);
public sealed record UserResponse(Guid Id, string Name, string Phone, string? Email, UserRole Role, UserStatus Status);
public sealed record DriverResponse(Guid Id, Guid UserId, string Name, string LicenseNumber, DriverOnboardingStatus OnboardingStatus, DriverAvailabilityStatus AvailabilityStatus, decimal? Rating, VehicleResponse? Vehicle);
public sealed record VehicleResponse(VehicleType Type, string Make, string Model, string RegistrationNumber);

[ApiController, Route("api/v1/drivers")]
public sealed class DriversController(DriverService drivers, ApplicationStore store) : ControllerBase
{
    [HttpPost("onboarding")] public ActionResult<DriverProfile> Onboard(OnboardingRequest request) => Ok(drivers.Onboard(request.UserId, request.LicenseNumber));
    [HttpPost("{driverId:guid}/approve")] public ActionResult<DriverProfile> Approve(Guid driverId) => Ok(drivers.Approve(ResolveDriverId(driverId)));
    [HttpPut("{driverId:guid}/availability")] public ActionResult<DriverProfile> Availability(Guid driverId, AvailabilityRequest request) => Ok(drivers.SetAvailability(ResolveDriverId(driverId), request.Status));
    [HttpPost("{driverId:guid}/vehicles")] public ActionResult<Vehicle> Vehicle(Guid driverId, VehicleRequest request) => Ok(drivers.AddVehicle(ResolveDriverId(driverId), request.VehicleType, request.Make, request.Model, request.RegistrationNumber, request.Color));
    [HttpGet("{driverId:guid}/rides/active")] public ActionResult<IReadOnlyList<RideResponse>> Active(Guid driverId) { var resolvedId = ResolveDriverId(driverId); return Ok(store.Rides.Values.Where(x => x.DriverId == resolvedId && x.Status is not (RideStatus.Completed or RideStatus.Cancelled)).Select(RidesController.ToResponse).ToArray()); }
    [HttpGet("{driverId:guid}/earnings")] public ActionResult<decimal> Earnings(Guid driverId) { var resolvedId = ResolveDriverId(driverId); return Ok(store.Payments.Values.Where(p => store.Rides.TryGetValue(p.RideId, out var r) && r.DriverId == resolvedId && p.Status == PaymentStatus.Succeeded).Sum(p => p.Amount)); }
    private Guid ResolveDriverId(Guid id) => store.Drivers.ContainsKey(id) ? id : store.Drivers.Values.FirstOrDefault(x => x.UserId == id)?.Id ?? id;
}
public sealed record OnboardingRequest(Guid UserId, string LicenseNumber);
public sealed record AvailabilityRequest(DriverAvailabilityStatus Status);
public sealed record VehicleRequest(VehicleType VehicleType, string Make, string Model, string RegistrationNumber, string? Color);

[ApiController, Route("api/v1/admin")]
public sealed class AdminController(ApplicationStore store) : ControllerBase
{
    [HttpGet("riders")] public IEnumerable<UserResponse> Riders() => store.Users.Values.Where(x => x.Role == UserRole.Rider).Select(x => new UserResponse(x.Id, $"{x.FirstName} {x.LastName}".Trim(), x.PhoneNumber, x.Email, x.Role, x.Status));
    [HttpGet("drivers")] public IEnumerable<DriverResponse> Drivers() => store.Drivers.Values.Select(x => new DriverResponse(x.Id, x.UserId, store.Users.TryGetValue(x.UserId, out var user) ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown driver", x.LicenseNumber, x.OnboardingStatus, x.AvailabilityStatus, x.Rating, store.Vehicles.Values.Where(v => v.DriverId == x.Id).Select(v => new VehicleResponse(v.VehicleType, v.Make, v.Model, v.RegistrationNumber)).FirstOrDefault()));
    [HttpGet("rides")] public IEnumerable<RideResponse> Rides() => store.Rides.Values.Select(RidesController.ToResponse);
    [HttpGet("payments")] public IEnumerable<Payment> Payments() => store.Payments.Values;
}