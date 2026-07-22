using Application.Payment.Features.Commands.AtomicRefundPayment;

namespace Application.Payment.Adapters;

public sealed class PaymentInitiator(ISender mediator) : IPaymentInitiator
{
    public Task<ServiceResult> InitiateRefundAsync(Guid orderId, string reason, CancellationToken ct)
        => mediator.Send(new AtomicRefundPaymentCommand(orderId, reason), ct);
}
