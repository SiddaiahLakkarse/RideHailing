using RideHailing.Domain;

namespace RideHailing.Application;

public interface IPaymentProvider
{
    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken ct);
    Task<PaymentResult> GetPaymentStatusAsync(string reference, CancellationToken ct);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct);
}
