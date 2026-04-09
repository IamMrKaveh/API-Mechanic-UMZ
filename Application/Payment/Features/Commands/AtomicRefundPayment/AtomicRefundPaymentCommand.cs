namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public record AtomicRefundPaymentCommand(
    Guid OrderId,
    Guid UserId,
    string Reason) : IRequest<ServiceResult>;