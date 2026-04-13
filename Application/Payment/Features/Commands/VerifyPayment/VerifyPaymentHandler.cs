using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Common.ValueObjects;
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
            result.Value.PaymentId,
            "VerifyPayment",
            IpAddress.Unknown,
            details: $"Authority: {request.Authority}");

        return ServiceResult<PaymentVerificationResult>.Success(result.Value);
    }
}