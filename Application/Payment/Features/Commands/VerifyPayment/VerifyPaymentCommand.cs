using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Commands.VerifyPayment;

public record VerifyPaymentCommand(string Authority, string Status) : IRequest<ServiceResult<PaymentVerificationResult>>;