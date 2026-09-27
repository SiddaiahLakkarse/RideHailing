using RideHailing.Domain;

namespace RideHailing.Application;

public sealed record AuthResult(Guid UserId, string AccessToken, string RefreshToken, UserRole Role);
