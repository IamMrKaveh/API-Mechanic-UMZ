using Application.Common.Results;
using Application.Payment.Features.Shared;

namespace Application.Payment.Contracts;

public interface IPaymentService
{
    Task<ServiceResult<PaymentInitiationResult>> InitiatePaymentAsync(
        Guid orderId,
        decimal amount,
        string ipAddress,
        CancellationToken ct = default);

    Task<ServiceResult<PaymentVerificationResult>> VerifyPaymentAsync(
        string authority,
        CancellationToken ct = default);

    Task<ServiceResult> ProcessWebhookAsync(
        string authority,
        string status,
        CancellationToken ct = default);
}