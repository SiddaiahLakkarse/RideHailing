using RideHailing.Infrastructure;
using RideHailing.Application;
using RideHailing.Domain;
namespace RideHailing.UnitTests;

public class UnitTest1
{
    [Fact]
    public void Fare_respects_minimum_and_tax() { var result = new FareCalculator().Calculate(new(3, 1, 1, 10, 2, .1m), 1, 1); Assert.Equal(10m, result.Total); }
    [Fact]
    public void Ride_rejects_invalid_transition() { var ride = new Ride(Guid.NewGuid(), VehicleType.Standard, new(1, 1), "a", new(2, 2), "b"); Assert.Throws<DomainException>(() => ride.TransitionTo(RideStatus.TripStarted)); }
    [Fact]
    public void Ride_assignment_is_single_winner() { var ride = new Ride(Guid.NewGuid(), VehicleType.Standard, new(1, 1), "a", new(2, 2), "b"); ride.TransitionTo(RideStatus.Matching); Assert.True(ride.TryAssign(Guid.NewGuid())); Assert.False(ride.TryAssign(Guid.NewGuid())); }
    [Fact]
    public void End_to_end_ride_workflow_completes_with_cash_and_rating()
    {
        var store = new ApplicationStore(); var rider = new User("+10000000001", null, "Rider", null, UserRole.Rider, "hash"); var driverUser = new User("+10000000002", null, "Driver", null, UserRole.Driver, "hash"); store.Users[rider.Id] = rider; store.Users[driverUser.Id] = driverUser;
        var driverService = new DriverService(store); var driver = driverService.Onboard(driverUser.Id, "LIC-1"); driverService.Approve(driver.Id); driverService.AddVehicle(driver.Id, VehicleType.Standard, "Make", "Model", "REG-1", null); driverService.SetAvailability(driver.Id, DriverAvailabilityStatus.Available);
        var workflow = new RideWorkflowService(store, new FareCalculator(), new FakePaymentProvider()); var ride = workflow.Create(rider.Id, new(VehicleType.Standard, new(17.38, 78.48), "Pickup", new(17.40, 78.50), "Destination", new(3, 1, .2m, 5, 1)), CancellationToken.None); workflow.Offer(ride.Id, driver.Id, TimeSpan.FromSeconds(15)); workflow.Accept(ride.Id, driver.Id); workflow.Advance(ride.Id, driver.Id, RideStatus.DriverArriving); workflow.Advance(ride.Id, driver.Id, RideStatus.DriverWaiting); workflow.Advance(ride.Id, driver.Id, RideStatus.TripStarted); workflow.Advance(ride.Id, driver.Id, RideStatus.TripCompleted); var payment = workflow.Pay(ride.Id, PaymentMethod.Cash); var rating = workflow.Rate(ride.Id, rider.Id, 5, "Great ride");
        Assert.Equal(PaymentStatus.Succeeded, payment.Status); Assert.Equal(RideStatus.Completed, ride.Status); Assert.Equal((byte)5, rating.Score);
    }
}

