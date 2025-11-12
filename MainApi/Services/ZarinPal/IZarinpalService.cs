namespace MainApi.Services.ZarinPal;

public interface IZarinpalService
{
    Task<ZarinpalRequestResponseDto?> CreatePaymentRequestAsync(decimal amount, string description, string callbackUrl, string? mobile = null, string? email = null);
    Task<ZarinpalVerificationResponseDataDto?> VerifyPaymentAsync(decimal amount, string authority);
    string GetPaymentGatewayUrl(string authority);
}