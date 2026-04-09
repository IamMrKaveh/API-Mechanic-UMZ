using Domain.Common.ValueObjects;

namespace Application.Payment.Contracts;

public interface IRefundableGateway : IPaymentGateway
{
    Task<ServiceResult<decimal>> RefundAsync(
        string authority,
        Money amount,
        string reason,
        CancellationToken ct = default);
}