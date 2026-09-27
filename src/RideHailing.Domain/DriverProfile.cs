namespace RideHailing.Domain;

public sealed class DriverProfile
{
    private DriverProfile() { }

    public DriverProfile(Guid userId, string licenseNumber) { Id = Guid.NewGuid(); UserId = userId; LicenseNumber = licenseNumber; CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow; }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string LicenseNumber { get; private set; } = "";
    public DriverOnboardingStatus OnboardingStatus { get; private set; } = DriverOnboardingStatus.Pending;
    public DriverAvailabilityStatus AvailabilityStatus { get; private set; } = DriverAvailabilityStatus.Offline;
    public decimal? Rating { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Approve() { OnboardingStatus = DriverOnboardingStatus.Approved; UpdatedAtUtc = DateTime.UtcNow; }

    public void SetAvailability(DriverAvailabilityStatus status)
    {
        if (status == DriverAvailabilityStatus.Available && OnboardingStatus != DriverOnboardingStatus.Approved) throw new DomainException("DRIVER_NOT_APPROVED", "Driver onboarding must be approved first.");
        AvailabilityStatus = status; UpdatedAtUtc = DateTime.UtcNow;
    }
}
