using Application.Payment.Features.Shared;

namespace Tests.Application.Payment.Features.Shared;

public class PaymentMethodDtosTests
{
    [Fact]
    public void PaymentMethodDto_Defaults_AreEmpty()
    {
        var dto = new PaymentMethodDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Name.ShouldBe(string.Empty);
        dto.Code.ShouldBe(string.Empty);
        dto.Description.ShouldBeNull();
        dto.IconUrl.ShouldBeNull();
        dto.FeeAmount.ShouldBe(0m);
        dto.FeePercentage.ShouldBe(0m);
    }

    [Fact]
    public void PaymentMethodDto_RoundTrip()
    {
        var dto = new PaymentMethodDto
        {
            Id = Guid.NewGuid(), Name = "Zarinpal", Code = "zarinpal",
            Description = "Online", IconUrl = "/icons/z.png",
            FeeAmount = 1_000m, FeePercentage = 1.5m, IsActive = true,
            SortOrder = 2, CreatedAt = new DateTime(2026, 1, 1)
        };

        dto.Code.ShouldBe("zarinpal");
        dto.FeePercentage.ShouldBe(1.5m);
    }

    [Fact]
    public void PaymentMethodListItemDto_RoundTrip()
    {
        var dto = new PaymentMethodListItemDto
        {
            Id = Guid.NewGuid(), Name = "Wallet", Code = "wallet",
            FeeAmount = 0m, FeePercentage = 0m,
            IsActive = true, IsDeleted = false, SortOrder = 1
        };

        dto.IsDeleted.ShouldBeFalse();
        dto.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void AvailablePaymentMethodDto_RoundTrip()
    {
        var dto = new AvailablePaymentMethodDto
        {
            Id = Guid.NewGuid(), Name = "Card", Code = "card",
            Fee = 2_000m, SortOrder = 0
        };

        dto.Fee.ShouldBe(2_000m);
        dto.Description.ShouldBeNull();
    }
}
