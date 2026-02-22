namespace Application.Payment.Features.Commands.InitiatePayment;

public record InitiatePaymentCommand(
    PaymentInitiationDto Dto,
    int UserId,
    string IpAddress
    ) : IRequest<ServiceResult<PaymentResultDto>>;