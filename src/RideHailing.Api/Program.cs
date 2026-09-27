using RideHailing.Application;
using RideHailing.Infrastructure;
using RideHailing.Domain;
using RideHailing.Api;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<FrontendDevelopmentServer>();
}
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<FareCalculator>();
builder.Services.AddSingleton<ApplicationStore>();
builder.Services.AddSingleton<AuthenticationService>();
builder.Services.AddSingleton<DriverService>();
builder.Services.AddSingleton<IRideRepository, InMemoryRideRepository>();
builder.Services.AddSingleton<IPaymentProvider, FakePaymentProvider>();
builder.Services.AddScoped<RideService>();
builder.Services.AddSingleton<RideWorkflowService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DomainException ex) when (!context.Response.HasStarted)
    {
        context.Response.StatusCode = ex.Code is "RIDE_OFFER_EXPIRED" or "RIDE_ALREADY_ASSIGNED" ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { code = ex.Code, message = ex.Message, traceId = context.TraceIdentifier });
    }
    catch (Exception ex) when (!context.Response.HasStarted)
    {
        app.Logger.LogError(ex, "Unhandled API exception for {Path}", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { code = "INTERNAL_ERROR", message = "An unexpected error occurred.", traceId = context.TraceIdentifier });
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    var authentication = app.Services.GetRequiredService<AuthenticationService>();
    var developmentUsers = new[]
    {
        new RegisterRequest("+1 555 0101", "alex@example.com", "Alex", "Morgan", "password", UserRole.Rider),
        new RegisterRequest("+1 555 0102", "jordan@example.com", "Jordan", "Lee", "password", UserRole.Driver),
        new RegisterRequest("+1 555 0103", "operations@example.com", "Operations", "Admin", "password", UserRole.Admin)
    };

    foreach (var user in developmentUsers)
    {
        try
        {
            await authentication.RegisterAsync(user, app.Lifetime.ApplicationStopping);
        }
        catch (DomainException ex) when (ex.Code == "PHONE_ALREADY_REGISTERED")
        {
        }
    }

    var developmentStore = app.Services.GetRequiredService<ApplicationStore>();
    var developmentDriverService = app.Services.GetRequiredService<DriverService>();
    var developmentDriverUser = developmentStore.Users.Values.First(x => x.Role == UserRole.Driver);
    if (!developmentStore.Drivers.Values.Any(x => x.UserId == developmentDriverUser.Id))
    {
        var developmentDriver = developmentDriverService.Onboard(developmentDriverUser.Id, "DL-482901");
        developmentDriverService.Approve(developmentDriver.Id);
        developmentDriverService.AddVehicle(developmentDriver.Id, VehicleType.Standard, "Toyota", "Camry", "RIDE-101", "Blue");
        developmentDriverService.SetAvailability(developmentDriver.Id, DriverAvailabilityStatus.Available);
    }
}

app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();
app.MapHub<RideHub>("/hubs/rides");

app.Run();

public sealed class RideHub : Microsoft.AspNetCore.SignalR.Hub
{
    public Task JoinRide(Guid rideId) => Groups.AddToGroupAsync(Context.ConnectionId, $"ride:{rideId}");
    public Task LeaveRide(Guid rideId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride:{rideId}");
}
