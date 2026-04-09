using Application.Payment.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Contracts;

public interface IPaymentGateway
{
    string GatewayName { get; }

    Task<ServiceResult<PaymentInitiationResult>> InitiateAsync(
        OrderId orderId,
        Money amount,
        string description,
        string callbackUrl,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CancellationToken ct = default);

    Task<ServiceResult<PaymentVerificationResult>> VerifyAsync(
        string authority,
        Money expectedAmount,
        CancellationToken ct = default);
}