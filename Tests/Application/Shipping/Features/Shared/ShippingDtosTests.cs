using Application.Shipping.Features.Shared;

namespace Tests.Application.Shipping.Features.Shared;

public class ShippingDtosTests
{
    [Fact]
    public void ShippingDto_Defaults_AreEmpty()
    {
        var dto = new ShippingDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Name.ShouldBe(string.Empty);
        dto.Description.ShouldBeNull();
        dto.BaseCost.ShouldBe(0m);
        dto.FreeShippingThreshold.ShouldBeNull();
        dto.RowVersion.ShouldBeNull();
    }

    [Fact]
    public void ShippingDto_RoundTrip()
    {
        var dto = new ShippingDto
        {
            Id = Guid.NewGuid(), Name = "Express", BaseCost = 80_000m,
            MinDeliveryDays = 1, MaxDeliveryDays = 2,
            IsActive = true, IsDefault = true, SortOrder = 1,
            FreeShippingThreshold = 500_000m,
            CreatedAt = new DateTime(2026, 1, 1)
        };

        dto.IsDefault.ShouldBeTrue();
        dto.FreeShippingThreshold.ShouldBe(500_000m);
    }

    [Fact]
    public void ShippingListItemDto_RoundTrip()
    {
        var dto = new ShippingListItemDto
        {
            Id = Guid.NewGuid(), Name = "Standard", BaseCost = 40_000m,
            IsActive = true, SortOrder = 0, DeliveryTimeDisplay = "2-4 days"
        };

        dto.DeliveryTimeDisplay.ShouldBe("2-4 days");
    }

    [Fact]
    public void ShippingQuoteItemDto_StoresValues()
    {
        var dto = new ShippingQuoteItemDto { VariantId = Guid.NewGuid(), Quantity = 2 };

        dto.Quantity.ShouldBe(2);
    }

    [Fact]
    public void ShippingCostResultDto_RoundTrip()
    {
        var dto = new ShippingCostResultDto
        {
            ShippingId = Guid.NewGuid(), ShippingName = "Express",
            Cost = 0m, IsFree = true, MinDeliveryDays = 1, MaxDeliveryDays = 1
        };

        dto.IsFree.ShouldBeTrue();
    }

    [Fact]
    public void AvailableShippingDto_RoundTrip()
    {
        var dto = new AvailableShippingDto
        {
            Id = Guid.NewGuid(), Name = "Express", Cost = 80_000m,
            IsFree = false, DeliveryTimeDisplay = "1-2 days", IsDefault = true
        };

        dto.Cost.ShouldBe(80_000m);
        dto.IsDefault.ShouldBeTrue();
    }
}
