using RideHailing.Domain;

namespace RideHailing.Application;

public interface IRideRepository
{
    Task<Ride?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(Ride ride, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
