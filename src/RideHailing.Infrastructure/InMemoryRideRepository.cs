using RideHailing.Application;
using RideHailing.Domain;

namespace RideHailing.Infrastructure;

public sealed class InMemoryRideRepository : IRideRepository
{
    private readonly Dictionary<Guid, Ride> store = [];

    public Task<Ride?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(store.GetValueOrDefault(id));
    public Task AddAsync(Ride ride, CancellationToken ct) { store[ride.Id] = ride; return Task.CompletedTask; }
    public Task SaveAsync(CancellationToken ct) => Task.CompletedTask;
}
