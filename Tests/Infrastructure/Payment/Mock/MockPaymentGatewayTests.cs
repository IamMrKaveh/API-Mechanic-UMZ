using Domain.Order.ValueObjects;
using Infrastructure.Payment.Mock;
using SharedKernel.ValueObjects;

namespace Tests.Infrastructure.Payment.Mock;

public class MockPaymentGatewayTests
{
    [Fact]
    public void GatewayName_Always_ReturnsMockGateway()
    {
        var sut = new MockPaymentGateway();

        sut.GatewayName.ShouldBe("MockGateway");
    }

    [Fact]
    public async Task InitiateAsync_WithAnyArguments_ReturnsAuthorityAndUrlContainingAmount()
    {
        var sut = new MockPaymentGateway();

        var result = await sut.InitiateAsync(
            OrderId.NewId(),
            Money.FromDecimal(12345m),
            "توضیحات آزمایشی",
            "https://example.com/callback");

        result.Authority.ShouldNotBeNullOrWhiteSpace();
        result.Authority.Length.ShouldBe(32);
        result.PaymentUrl.ShouldContain("authority=");
        result.PaymentUrl.ShouldContain("amount=12345");
    }

    [Fact]
    public async Task VerifyAsync_WithAnyArguments_ReturnsVerifiedResultWithMaskedPan()
    {
        var sut = new MockPaymentGateway();

        var result = await sut.VerifyAsync("any-authority", Money.FromDecimal(1000m));

        result.IsVerified.ShouldBeTrue();
        result.RefId.ShouldNotBeNull();
        result.RefId!.Value.ShouldBeGreaterThan(0L);
        result.CardPan.ShouldBe("6037********1234");
        result.Fee.ShouldBe(0m);
    }
}
