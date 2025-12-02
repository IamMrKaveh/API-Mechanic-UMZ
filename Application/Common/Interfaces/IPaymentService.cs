namespace Application.Common.Interfaces;


public interface IPaymentService
{
    Task<(string? PaymentUrl, string? Authority, string? Error)> InitiatePaymentAsync(
    int orderId,
    int userId,
    decimal amount,
    string description,
    string? mobile,
    string? email,
    string gatewayName);


    Task<PaymentVerificationResultDto> VerifyPaymentAsync(string authority, string status);


    Task ProcessGatewayWebhookAsync(string gatewayName, string authority, string status, long? refId);


    Task CleanupAbandonedPaymentsAsync(CancellationToken cancellationToken);
}