using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Contracts;

public interface IPaymentService
{
    Task<ServiceResult<PaymentInitiationResult>> InitiatePaymentAsync(
        OrderId orderId,
        Money amount,
        IpAddress ipAddress,
        UserId userId,
        CancellationToken ct = default);

    Task<ServiceResult<PaymentVerificationResult>> VerifyPaymentAsync(
        string authority,
        CancellationToken ct = default);

    Task<ServiceResult> ProcessWebhookAsync(
        string authority,
        string status,
        CancellationToken ct = default);
}