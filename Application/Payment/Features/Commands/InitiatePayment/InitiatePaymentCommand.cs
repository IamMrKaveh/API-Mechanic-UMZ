using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Commands.InitiatePayment;

public record InitiatePaymentCommand(
    Guid OrderId,
    Guid UserId,
    string? GatewayName,
    string IpAddress) : IRequest<ServiceResult<PaymentInitiationResult>>;