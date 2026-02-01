using Application.DTOs.Payment;

namespace Application.Common.Interfaces.Payment;

public interface IPaymentService
{
    Task<PaymentRequestResultDto> InitiatePaymentAsync(PaymentInitiationDto dto);

    Task<PaymentVerificationResultDto> VerifyPaymentAsync(string authority, string status);

    Task ProcessGatewayWebhookAsync(string gatewayName, string authority, string status, long? refId);

    Task CleanupAbandonedPaymentsAsync(CancellationToken cancellationToken);
}