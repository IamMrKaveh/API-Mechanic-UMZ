namespace Application.Payment.Features.Commands.VerifyPayment;

public record VerifyPaymentCommand(string Authority, string Status) : IRequest<ServiceResult<PaymentResultDto>>;