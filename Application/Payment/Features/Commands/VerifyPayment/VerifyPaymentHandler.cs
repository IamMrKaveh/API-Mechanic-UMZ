using Application.Payment.Features.Shared;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentHandler(
    IPaymentService paymentService,
    IAuditService auditService) : IRequestHandler<VerifyPaymentCommand, ServiceResult<PaymentVerificationResult>>
{
    public async Task<ServiceResult<PaymentVerificationResult>> Handle(
        VerifyPaymentCommand request,
        CancellationToken ct)
    {
        var result = await paymentService.VerifyPaymentAsync(request.Authority, ct);

        if (!result.IsSuccess)
            return ServiceResult<PaymentVerificationResult>.Failure(result.Error);

        await auditService.LogPaymentEventAsync(
            PaymentTransactionId.From(result.Value.TransactionId.Value),
            "VerifyPayment",
            IpAddress.Unknown,
            details: $"Authority: {request.Authority}",
            ct: ct);

        return ServiceResult<PaymentVerificationResult>.Success(result.Value);
    }
}