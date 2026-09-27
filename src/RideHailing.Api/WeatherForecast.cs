using Microsoft.AspNetCore.SignalR;
using RideHailing.Application;
using RideHailing.Domain;
namespace RideHailing.Api;
public sealed class RideHub(ApplicationStore store, RideWorkflowService workflow) : Hub
{
    public Task JoinRide(Guid rideId) => store.Rides.ContainsKey(rideId) ? Groups.AddToGroupAsync(Context.ConnectionId, $"ride:{rideId}") : Task.FromException(new DomainException("RIDE_NOT_FOUND", "Ride was not found."));
    public Task LeaveRide(Guid rideId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride:{rideId}");
    public async Task UpdateDriverLocation(Guid rideId, Guid driverId, double latitude, double longitude, double accuracyMeters, double? heading = null, double? speedMps = null) { workflow.UpdateLocation(driverId, rideId, new(latitude, longitude), accuracyMeters, heading, speedMps); await Clients.Group($"ride:{rideId}").SendAsync("DriverLocationUpdated", new { driverId, latitude, longitude, accuracyMeters, heading, speedMps, updatedAtUtc = DateTime.UtcNow }); }
}
