using Application.Payment.Features.Shared;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentHandler(
    IPaymentService paymentService,
    IAuditService auditService)
    : ICommandHandler<VerifyPaymentCommand, PaymentVerificationResult>
{
    public async Task<ServiceResult<PaymentVerificationResult>> Handle(
        VerifyPaymentCommand request,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Status)
            && !request.Status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            await auditService.LogWarningAsync(
                $"[VerifyPayment] User cancelled payment. Authority: {request.Authority}, Status: {request.Status}",
                ct);
            return ServiceResult<PaymentVerificationResult>.Failure("پرداخت توسط کاربر لغو شد.");
        }

        var result = await paymentService.VerifyPaymentAsync(request.Authority, ct);

        if (!result.IsSuccess || result.Value is null)
            return ServiceResult<PaymentVerificationResult>.Failure(result.Error ?? "پرداخت تأیید نشد.");

        if (result.Value.TransactionId.HasValue)
        {
            await auditService.LogPaymentEventAsync(
                PaymentTransactionId.From(result.Value.TransactionId.Value),
                "VerifyPayment",
                IpAddress.Unknown,
                details: $"Authority: {request.Authority}",
                ct: ct);
        }

        return ServiceResult<PaymentVerificationResult>.Success(result.Value);
    }
}