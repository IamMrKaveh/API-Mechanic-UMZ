using Application.Discount.Features.Shared;

namespace Tests.Application.Discount.Features.Shared;

public class DiscountDtosTests
{
    [Fact]
    public void DiscountValidationResult_Defaults_AreEmpty()
    {
        var dto = new DiscountValidationResult();

        dto.DiscountCodeId.ShouldBe(default(Guid));
        dto.Code.ShouldBe(string.Empty);
        dto.DiscountAmount.ShouldBe(0m);
        dto.IsValid.ShouldBeFalse();
        dto.Error.ShouldBeNull();
    }

    [Fact]
    public void DiscountApplicationResult_SuccessShape_RoundTrip()
    {
        var dto = new DiscountApplicationResult { IsSuccess = true, DiscountAmount = 20_000m, FinalAmount = 180_000m };

        dto.IsSuccess.ShouldBeTrue();
        dto.DiscountAmount.ShouldBe(20_000m);
        dto.FinalAmount.ShouldBe(180_000m);
        dto.Error.ShouldBeNull();
    }

    [Fact]
    public void DiscountDto_Defaults_AreEmpty()
    {
        var dto = new DiscountDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Code.ShouldBe(string.Empty);
        dto.MaximumDiscountAmount.ShouldBeNull();
        dto.UsageLimit.ShouldBeNull();
        dto.StartsAt.ShouldBeNull();
        dto.ExpiresAt.ShouldBeNull();
        dto.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void DiscountCodeDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var dto = new DiscountCodeDto
        {
            Id = id, Code = "SAVE10", DiscountType = "Percentage", DiscountValue = 10m,
            UsageLimit = 100, UsageCount = 3, IsActive = true, IsRedeemable = true,
            ExpiresAt = new DateTime(2026, 12, 31), CreatedAt = new DateTime(2026, 1, 1)
        };

        dto.Id.ShouldBe(id);
        dto.Code.ShouldBe("SAVE10");
        dto.UsageCount.ShouldBe(3);
    }

    [Fact]
    public void DiscountCodeDetailDto_DefaultRestrictions_IsEmpty()
    {
        var dto = new DiscountCodeDetailDto();

        dto.Restrictions.ShouldNotBeNull();
        dto.Restrictions.ShouldBeEmpty();
    }

    [Fact]
    public void DiscountRestrictionDto_RoundTrip()
    {
        var dto = new DiscountRestrictionDto { Id = Guid.NewGuid(), RestrictionType = "MinimumOrderAmount", RestrictionValue = "100000" };

        dto.RestrictionType.ShouldBe("MinimumOrderAmount");
        dto.RestrictionValue.ShouldBe("100000");
    }

    [Fact]
    public void DiscountInfoDto_RoundTrip()
    {
        var dto = new DiscountInfoDto
        {
            Code = "X", DiscountType = "FixedAmount", DiscountValue = 5_000m,
            MaximumDiscountAmount = 5_000m, ExpiresAt = new DateTime(2026, 6, 1), IsRedeemable = true
        };

        dto.IsRedeemable.ShouldBeTrue();
        dto.DiscountValue.ShouldBe(5_000m);
    }

    [Fact]
    public void DiscountUsageReportDto_DefaultUsages_IsEmpty()
    {
        var dto = new DiscountUsageReportDto();

        dto.Usages.ShouldNotBeNull();
        dto.Usages.ShouldBeEmpty();
        dto.TotalUsages.ShouldBe(0);
    }

    [Fact]
    public void DiscountUsageItemDto_RoundTrip()
    {
        var dto = new DiscountUsageItemDto
        {
            UserId = Guid.NewGuid(), OrderId = Guid.NewGuid(),
            DiscountedAmount = 15_000m, UsedAt = new DateTime(2026, 2, 2)
        };

        dto.DiscountedAmount.ShouldBe(15_000m);
    }
}
