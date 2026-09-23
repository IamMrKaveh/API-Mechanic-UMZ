using Application.Payment.Features.Shared;

namespace Tests.Application.Payment.Features.Shared;

public class PaymentDtosTests
{
    [Fact]
    public void PaymentTransactionDto_Defaults_AreEmpty()
    {
        var dto = new PaymentTransactionDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Authority.ShouldBe(string.Empty);
        dto.Gateway.ShouldBe(string.Empty);
        dto.Amount.ShouldBe(0m);
        dto.Status.ShouldBe(string.Empty);
        dto.StatusDisplayName.ShouldBe(string.Empty);
        dto.RefId.ShouldBeNull();
        dto.IsSuccessful.ShouldBeFalse();
        dto.VerifiedAt.ShouldBeNull();
    }

    [Fact]
    public void PaymentTransactionDto_RoundTrip()
    {
        var dto = new PaymentTransactionDto
        {
            Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            Authority = "AUTH-1", Gateway = "Zarinpal", Amount = 500_000m,
            Status = "Verified", StatusDisplayName = "تأیید شده",
            RefId = 123456L, IsSuccessful = true,
            VerifiedAt = new DateTime(2026, 1, 2), ExpiresAt = new DateTime(2026, 1, 3),
            CreatedAt = new DateTime(2026, 1, 1)
        };

        dto.Authority.ShouldBe("AUTH-1");
        dto.RefId.ShouldBe(123456L);
        dto.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void PaymentInitiationResult_StoresValues()
    {
        var result = new PaymentInitiationResult("AUTH-9", "https://pay/x", Guid.NewGuid());

        result.Authority.ShouldBe("AUTH-9");
        result.PaymentUrl.ShouldBe("https://pay/x");
    }

    [Fact]
    public void PaymentVerificationResult_StoresValues()
    {
        var result = new PaymentVerificationResult(Guid.NewGuid(), true, 777L, "603799****1234", 5_000m);

        result.IsVerified.ShouldBeTrue();
        result.RefId.ShouldBe(777L);
        result.Fee.ShouldBe(5_000m);
    }

    [Fact]
    public void PaymentStatusDto_RoundTrip()
    {
        var dto = new PaymentStatusDto
        {
            Authority = "A1", Status = "Paid", IsSuccess = true, RefId = 5L, Amount = 100m
        };

        dto.IsSuccess.ShouldBeTrue();
        dto.Amount.ShouldBe(100m);
    }
}
