namespace RideHailing.Domain;

public sealed class User
{
    private User() { }

    public User(string phoneNumber, string? email, string firstName, string? lastName, UserRole role, string passwordHash)
    { Id = Guid.NewGuid(); PhoneNumber = phoneNumber; Email = email; FirstName = firstName; LastName = lastName; Role = role; PasswordHash = passwordHash; CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow; }

    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = "";
    public string? Email { get; private set; }
    public string FirstName { get; private set; } = "";
    public string? LastName { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public string PasswordHash { get; private set; } = "";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
