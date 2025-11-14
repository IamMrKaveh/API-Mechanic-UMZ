namespace Application.Common.Interfaces;

public interface IZarinpalService
{
    Task<ZarinpalRequestResponseDto?> CreatePaymentRequestAsync(ZarinpalSettingsDto settings, decimal amount, string description, string callbackUrl, string? mobile = null, string? email = null);
    Task<ZarinpalVerificationResponseDataDto?> VerifyPaymentAsync(ZarinpalSettingsDto settings, decimal amount, string authority);
    string GetPaymentGatewayUrl(bool isSandbox, string authority);
    Task<(string? PaymentUrl, string? ErrorMessage)> RequestPaymentAsync(decimal amount, string description, int orderId, int userId, string callbackUrl, string? userPhone);
}