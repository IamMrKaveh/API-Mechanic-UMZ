namespace Application.Payment.Contracts;

public interface IPaymentInitiator
{
    Task<ServiceResult> InitiateRefundAsync(Guid orderId, string reason, CancellationToken ct);
}
