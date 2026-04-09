namespace Application.Payment.Contracts;

public interface IRefundableGateway : IPaymentGateway
{
    Task<ServiceResult<decimal>> RefundAsync(
        string authority,
        decimal amount,
        string reason,
        CancellationToken ct = default);
}