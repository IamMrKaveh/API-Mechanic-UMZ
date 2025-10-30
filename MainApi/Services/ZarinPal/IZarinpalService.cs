namespace MainApi.Services.ZarinPal;

public interface IZarinpalService
{
    Task<(string? paymentUrl, string? authority)> RequestPaymentAsync(decimal amount, string description, string callbackUrl, string? mobile, string? email);
    Task<(bool isSuccess, long? refId)> VerifyPaymentAsync(string authority, decimal amount);
}