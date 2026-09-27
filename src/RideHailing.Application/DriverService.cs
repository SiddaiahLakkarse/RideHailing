using RideHailing.Domain;

namespace RideHailing.Application;

public sealed class DriverService(ApplicationStore store)
{
    public DriverProfile Onboard(Guid userId, string license) { if (!store.Users.ContainsKey(userId)) throw new DomainException("USER_NOT_FOUND", "User was not found."); var driver = new DriverProfile(userId, license); store.Drivers[driver.Id] = driver; return driver; }
    public Vehicle AddVehicle(Guid driverId, VehicleType type, string make, string model, string registration, string? color) { EnsureDriver(driverId); var vehicle = new Vehicle(driverId, type, make, model, registration, color); store.Vehicles[vehicle.Id] = vehicle; return vehicle; }
    public DriverProfile SetAvailability(Guid driverId, DriverAvailabilityStatus status) { var driver = EnsureDriver(driverId); driver.SetAvailability(status); return driver; }
    public DriverProfile Approve(Guid driverId) { var driver = EnsureDriver(driverId); driver.Approve(); return driver; }
    private DriverProfile EnsureDriver(Guid id) => store.Drivers.TryGetValue(id, out var driver) ? driver : throw new DomainException("DRIVER_NOT_FOUND", "Driver was not found.");
}
