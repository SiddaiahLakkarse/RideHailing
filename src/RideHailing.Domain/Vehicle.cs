namespace RideHailing.Domain;

public sealed class Vehicle
{
    private Vehicle() { }

    public Vehicle(Guid driverId, VehicleType type, string make, string model, string registrationNumber, string? color) { Id = Guid.NewGuid(); DriverId = driverId; VehicleType = type; Make = make; Model = model; RegistrationNumber = registrationNumber; Color = color; CreatedAtUtc = DateTime.UtcNow; }

    public Guid Id { get; private set; }
    public Guid DriverId { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public string Make { get; private set; } = "";
    public string Model { get; private set; } = "";
    public string RegistrationNumber { get; private set; } = "";
    public string? Color { get; private set; }
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;
    public DateTime CreatedAtUtc { get; private set; }
}
