using Application.Payment.Features.Shared;
using Domain.Order.Exceptions;
using Domain.Payment.Exceptions;
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

        PaymentVerificationResult result;
        try
        {
            result = await paymentService.VerifyPaymentAsync(request.Authority, ct);
        }
        catch (PaymentTransactionNotFoundException ex)
        {
            return ServiceResult<PaymentVerificationResult>.NotFound(ex.Message);
        }
        catch (OrderNotFoundException ex)
        {
            return ServiceResult<PaymentVerificationResult>.NotFound(ex.Message);
        }
        catch (PaymentNotVerifiableException ex)
        {
            return ServiceResult<PaymentVerificationResult>.Failure(ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            return ServiceResult<PaymentVerificationResult>.Failure(ex.Message);
        }

        if (result.TransactionId.HasValue)
        {
            await auditService.LogPaymentEventAsync(
                PaymentTransactionId.From(result.TransactionId.Value),
                "VerifyPayment",
                IpAddress.Unknown,
                details: $"Authority: {request.Authority}",
                ct: ct);
        }

        return ServiceResult<PaymentVerificationResult>.Success(result);
    }
}