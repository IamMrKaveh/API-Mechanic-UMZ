namespace Infrastructure.Payment.Mock;

public class MockPaymentGateway : IPaymentGateway
{
    public string GatewayName => "MockGateway";

    public Task<PaymentRequestResultDto> RequestPaymentAsync(decimal amount, string description, string callbackUrl, string? mobile, string? email)
    {
        var authority = Guid.NewGuid().ToString();

        // We use a relative URL here which the browser will resolve against the API domain.
        // This assumes the API hosts the mock page at this route.
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
        // In a real mock, we might check if 'authority' exists in a dictionary,
        // but for a simple start, we assume all verifies are successful if they reach this stage.
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