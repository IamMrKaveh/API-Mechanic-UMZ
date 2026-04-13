using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;

namespace Application.Payment.Contracts;

public interface IPaymentService
{
    Task<ServiceResult<PaymentInitiationResult>> InitiatePaymentAsync(
        OrderId orderId,
        Money amount,
        IpAddress ipAddress,
        CancellationToken ct = default);

    Task<ServiceResult<PaymentVerificationResult>> VerifyPaymentAsync(
        string authority,
        CancellationToken ct = default);

    Task<ServiceResult> ProcessWebhookAsync(
        string authority,
        string status,
        CancellationToken ct = default);
}