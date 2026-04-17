using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Payment.Mock;

public sealed class MockPaymentGateway : IPaymentGateway
{
    public string GatewayName => "MockGateway";

    public Task<ServiceResult<PaymentInitiationResult>> InitiateAsync(
        OrderId orderId,
        Money amount,
        string description,
        string callbackUrl,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CancellationToken ct = default)
    {
        var authority = Guid.NewGuid().ToString("N");
        var paymentUrl = $"/mock/pay?authority={authority}&amount={amount.Amount}";

        var result = new PaymentInitiationResult(authority, paymentUrl);
        return Task.FromResult(ServiceResult<PaymentInitiationResult>.Success(result));
    }

    public Task<ServiceResult<PaymentVerificationResult>> VerifyAsync(
        string authority,
        Money expectedAmount,
        CancellationToken ct = default)
    {
        var result = new PaymentVerificationResult(
            TransactionId: null,
            IsVerified: true,
            RefId: DateTime.UtcNow.Ticks,
            CardPan: "6037********1234",
            Fee: 0);

        return Task.FromResult(ServiceResult<PaymentVerificationResult>.Success(result));
    }
}