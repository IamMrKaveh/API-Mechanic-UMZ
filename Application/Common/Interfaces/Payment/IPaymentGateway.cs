using Application.DTOs.Payment;

namespace Application.Common.Interfaces.Payment;

public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PaymentRequestResultDto> RequestPaymentAsync(decimal amount, string description, string callbackUrl, string? mobile, string? email);
    Task<GatewayVerificationResultDto> VerifyPaymentAsync(string authority, int amount);
}