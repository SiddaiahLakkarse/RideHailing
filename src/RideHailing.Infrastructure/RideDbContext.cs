using Microsoft.EntityFrameworkCore;
using RideHailing.Domain;

namespace RideHailing.Infrastructure;

public sealed class RideDbContext(DbContextOptions<RideDbContext> options) : DbContext(options)
{
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<User> Users => Set<User>();
    public DbSet<DriverProfile> Drivers => Set<DriverProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Rating> Ratings => Set<Rating>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Ride>(e => { e.HasKey(x => x.Id); e.Property(x => x.Status).HasConversion<int>(); e.Property(x => x.VehicleType).HasConversion<int>(); e.Property(x => x.Pickup).HasConversion(p => $"{p.Latitude},{p.Longitude}", s => Parse(s)); e.Property(x => x.Destination).HasConversion(p => $"{p.Latitude},{p.Longitude}", s => Parse(s)); e.Property<byte[]>("RowVersion").IsRowVersion(); });
        b.Entity<User>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.PhoneNumber).IsUnique(); e.Property(x => x.Role).HasConversion<int>(); e.Property(x => x.Status).HasConversion<int>(); });
        b.Entity<DriverProfile>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); e.Property(x => x.OnboardingStatus).HasConversion<int>(); e.Property(x => x.AvailabilityStatus).HasConversion<int>(); });
        b.Entity<Vehicle>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.RegistrationNumber).IsUnique(); e.Property(x => x.VehicleType).HasConversion<int>(); e.Property(x => x.Status).HasConversion<int>(); });
        b.Entity<Payment>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.ProviderReference }).IsUnique().HasFilter("[ProviderReference] IS NOT NULL"); e.Property(x => x.Method).HasConversion<int>(); e.Property(x => x.Status).HasConversion<int>(); });
        b.Entity<Rating>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.RideId, x.RaterUserId, x.RatedUserId }).IsUnique(); });
    }

    private static GeoPoint Parse(string value) { var p = value.Split(','); return new(double.Parse(p[0]), double.Parse(p[1])); }
}
