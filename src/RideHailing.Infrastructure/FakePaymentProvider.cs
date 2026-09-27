using RideHailing.Application;
using RideHailing.Domain;

namespace RideHailing.Infrastructure;

public sealed class FakePaymentProvider : IPaymentProvider
{
    public Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken ct) => Task.FromResult(new PaymentResult(PaymentStatus.Succeeded, $"dev_{request.IdempotencyKey}"));
    public Task<PaymentResult> GetPaymentStatusAsync(string reference, CancellationToken ct) => Task.FromResult(new PaymentResult(PaymentStatus.Succeeded, reference));
    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct) => Task.FromResult(new RefundResult(true, $"refund_{request.PaymentId}"));
}
