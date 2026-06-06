namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public record AtomicRefundPaymentCommand(
    Guid OrderId,
    string Reason) : IRequest<ServiceResult>;