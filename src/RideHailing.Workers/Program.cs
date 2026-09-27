using RideHailing.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<RideMatchingWorker>();
builder.Services.AddHostedService<PaymentReconciliationWorker>();
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddHostedService<ExpiredRideOfferWorker>();
builder.Services.AddHostedService<DriverLocationCleanupWorker>();
builder.Services.AddHostedService<OutboxPublisherWorker>();

var host = builder.Build();
host.Run();
