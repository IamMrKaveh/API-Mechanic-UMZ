namespace Application.Payment.Features.Commands.InitiatePayment;

public record InitiatePaymentCommand(InitiatePaymentDto Dto, int UserId, string IpAddress) : IRequest<ServiceResult<PaymentResultDto>>;