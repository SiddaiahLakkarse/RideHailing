using RideHailing.Domain;

namespace RideHailing.Application;

public sealed class AuthenticationService(ApplicationStore store)
{
    public Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct) { ValidatePassword(request.Password); if (store.Users.Values.Any(x => x.PhoneNumber == request.PhoneNumber)) throw new DomainException("PHONE_ALREADY_REGISTERED", "Phone number is already registered."); var user = new User(request.PhoneNumber, request.Email, request.FirstName, request.LastName, request.Role, Hash(request.Password)); store.Users[user.Id] = user; return Task.FromResult(Issue(user)); }
    public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct) { var user = store.Users.Values.SingleOrDefault(x => x.PhoneNumber == request.PhoneNumber && x.PasswordHash == Hash(request.Password)) ?? throw new DomainException("INVALID_CREDENTIALS", "Invalid phone number or password."); return Task.FromResult(Issue(user)); }
    public Task<AuthResult> RefreshAsync(string token, CancellationToken ct) { if (!store.RefreshTokens.TryRemove(token, out var id) || !store.Users.TryGetValue(id, out var user)) throw new DomainException("INVALID_REFRESH_TOKEN", "Refresh token is invalid."); return Task.FromResult(Issue(user)); }
    public void Logout(string token) => store.RefreshTokens.TryRemove(token, out _);
    private AuthResult Issue(User user) { var access = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); var refresh = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); store.RefreshTokens[refresh] = user.Id; return new(user.Id, access, refresh, user.Role); }
    private static void ValidatePassword(string password)
    {
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit))
            throw new DomainException("WEAK_PASSWORD", "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
    }
    private static string Hash(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}
