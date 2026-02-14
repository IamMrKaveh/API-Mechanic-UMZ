namespace Application.Payment.Features.Commands.RefundPayment;

public record RefundPaymentCommand(
    int TransactionId,
    string? Reason,
    int AdminUserId) : IRequest<ServiceResult<PaymentResultDto>>;