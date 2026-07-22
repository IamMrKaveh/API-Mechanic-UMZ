using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Contracts;

public interface IPaymentService
{
    Task<PaymentInitiationResult> InitiatePaymentAsync(
        OrderId orderId,
        Money amount,
        IpAddress ipAddress,
        UserId userId,
        string? gatewayName = null,
        CancellationToken ct = default);

    Task<PaymentVerificationResult> VerifyPaymentAsync(
        string authority,
        CancellationToken ct = default);

    Task ProcessWebhookAsync(
        string authority,
        string status,
        string? nonce = null,
        CancellationToken ct = default);
}
