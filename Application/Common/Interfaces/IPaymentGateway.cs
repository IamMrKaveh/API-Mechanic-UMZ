namespace Application.Common.Interfaces;

public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PaymentRequestResultDto> RequestPaymentAsync(PaymentInitiationDto details);
    Task<GatewayVerificationResultDto> VerifyPaymentAsync(decimal amount, string authority);
}