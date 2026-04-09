using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentHandler(
    IPaymentService paymentService,
    ILogger<VerifyPaymentHandler> logger) : IRequestHandler<VerifyPaymentCommand, ServiceResult<PaymentVerificationResult>>
{
    public async Task<ServiceResult<PaymentVerificationResult>> Handle(
        VerifyPaymentCommand request, CancellationToken ct)
    {
        if (request.Status != "OK")
        {
            logger.LogWarning("Payment cancelled by user for authority {Authority}", request.Authority);
            return ServiceResult<PaymentVerificationResult>.Failure("پرداخت توسط کاربر لغو شد.");
        }

        return await paymentService.VerifyPaymentAsync(request.Authority, ct);
    }
}