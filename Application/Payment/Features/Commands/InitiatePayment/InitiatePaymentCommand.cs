using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Commands.InitiatePayment;

public record InitiatePaymentCommand(
    Guid OrderId,
    string? GatewayName)
    : ICommand<PaymentInitiationResult>;