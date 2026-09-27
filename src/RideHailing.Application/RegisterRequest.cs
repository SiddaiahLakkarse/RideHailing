using RideHailing.Domain;

namespace RideHailing.Application;

public sealed record RegisterRequest(string PhoneNumber, string? Email, string FirstName, string? LastName, string Password, UserRole Role);
