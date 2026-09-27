namespace RideHailing.Workers;

public class Worker(ILogger<Worker> logger) : BackgroundService { protected override async Task ExecuteAsync(CancellationToken token) { while (!token.IsCancellationRequested) { logger.LogInformation("RideHailing worker cycle at {time}", DateTimeOffset.UtcNow); await Task.Delay(TimeSpan.FromSeconds(5), token); } } }
public abstract class IntervalWorker(ILogger logger, TimeSpan interval) : BackgroundService { protected override async Task ExecuteAsync(CancellationToken token) { while (!token.IsCancellationRequested) { try { await ExecuteCycleAsync(token); } catch (OperationCanceledException) when (token.IsCancellationRequested) { } catch (Exception ex) { logger.LogError(ex, "Worker cycle failed for {Worker}", GetType().Name); } await Task.Delay(interval, token); } } protected virtual Task ExecuteCycleAsync(CancellationToken token) => Task.CompletedTask; }
public sealed class RideMatchingWorker(ILogger<RideMatchingWorker> logger) : IntervalWorker(logger, TimeSpan.FromSeconds(2)) { }
public sealed class PaymentReconciliationWorker(ILogger<PaymentReconciliationWorker> logger) : IntervalWorker(logger, TimeSpan.FromSeconds(10)) { }
public sealed class NotificationWorker(ILogger<NotificationWorker> logger) : IntervalWorker(logger, TimeSpan.FromSeconds(2)) { }
public sealed class ExpiredRideOfferWorker(ILogger<ExpiredRideOfferWorker> logger) : IntervalWorker(logger, TimeSpan.FromSeconds(5)) { }
public sealed class DriverLocationCleanupWorker(ILogger<DriverLocationCleanupWorker> logger) : IntervalWorker(logger, TimeSpan.FromMinutes(1)) { }
public sealed class OutboxPublisherWorker(ILogger<OutboxPublisherWorker> logger) : IntervalWorker(logger, TimeSpan.FromSeconds(5)) { }

