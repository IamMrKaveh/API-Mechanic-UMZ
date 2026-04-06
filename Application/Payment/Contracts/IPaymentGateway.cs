using Application.Common.Results;
using Application.Payment.Features.Shared;

namespace Application.Payment.Contracts;

public interface IPaymentGateway
{
    string GatewayName { get; }

    Task<ServiceResult<PaymentInitiationResult>> InitiateAsync(
        Guid orderId,
        decimal amount,
        string description,
        string callbackUrl,
        string? email = null,
        string? mobile = null,
        CancellationToken ct = default);

    Task<ServiceResult<PaymentVerificationResult>> VerifyAsync(
        string authority,
        decimal expectedAmount,
        CancellationToken ct = default);
}