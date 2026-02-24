namespace Infrastructure.Payment.Mock;

public class MockPaymentGateway : IPaymentGateway
{
    public string GatewayName => "MockGateway";

    public Task<PaymentRequestResultDto> RequestPaymentAsync(
        decimal amount,
        string description,
        string callbackUrl,
        string? mobile = null,
        string? email = null)
    {
        var authority = Guid.NewGuid().ToString();

        var paymentUrl = $"/api/mock-gateway/pay?amount={amount}&authority={authority}&callback={System.Net.WebUtility.UrlEncode(callbackUrl)}";

        return Task.FromResult(new PaymentRequestResultDto
        {
            IsSuccess = true,
            Authority = authority,
            PaymentUrl = paymentUrl,
            RawResponse = "Mock Request Success",
            RedirectUrl = paymentUrl
        });
    }

    public Task<GatewayVerificationResultDto> VerifyPaymentAsync(string authority, int amount)
    {
        return Task.FromResult(new GatewayVerificationResultDto
        {
            IsVerified = true,
            RefId = DateTime.UtcNow.Ticks,
            CardPan = "6037********1234",
            CardHash = "mock-hash-123456",
            Fee = 0,
            Message = "پرداخت تستی با موفقیت تایید شد.",
            RawResponse = "{\"status\": \"success\", \"mock\": true}"
        });
    }
}