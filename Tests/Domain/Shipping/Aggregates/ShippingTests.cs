using Domain.Shipping.Events;
using Domain.Shipping.Exceptions;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Domain.Shipping.Aggregates;

public class ShippingTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedShippingWithDefaults()
    {
        var sut = new ShippingBuilder()
            .WithName("پست پیشتاز")
            .WithBaseCost(50_000m)
            .WithDescription("ارسال سریع")
            .WithEstimatedDeliveryTime("۲ تا ۴ روز")
            .WithDeliveryDays(2, 4)
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.Name.Value.ShouldBe("پست پیشتاز");
        sut.BaseCost.Amount.ShouldBe(50_000m);
        sut.Description.ShouldBe("ارسال سریع");
        sut.EstimatedDeliveryTime.ShouldBe("۲ تا ۴ روز");
        sut.DeliveryTime.MinDays.ShouldBe(2);
        sut.DeliveryTime.MaxDays.ShouldBe(4);
        sut.IsActive.ShouldBeTrue();
        sut.IsDefault.ShouldBeFalse();
        sut.IsDeleted.ShouldBeFalse();
        sut.SortOrder.ShouldBe(0);
        sut.OrderRange.HasMinimum.ShouldBeFalse();
        sut.OrderRange.HasMaximum.ShouldBeFalse();
        sut.FreeShipping.IsEnabled.ShouldBeFalse();
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_TrimsDescriptionAndEstimatedDeliveryTime()
    {
        var sut = new ShippingBuilder()
            .WithDescription("  desc  ")
            .WithEstimatedDeliveryTime("  eta  ")
            .Build();

        sut.Description.ShouldBe("desc");
        sut.EstimatedDeliveryTime.ShouldBe("eta");
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new ShippingBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_ProducesAggregateWithVersionOne()
    {
        new ShippingBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneShippingCreatedEventWithNameAndBaseCost()
    {
        var sut = new ShippingBuilder()
            .WithName("Post")
            .WithBaseCost(75_000m)
            .Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<ShippingCreatedEvent>();
        evt.ShippingId.ShouldBe(sut.Id);
        evt.Name.Value.ShouldBe("Post");
        evt.BaseCost.ShouldBe(75_000m);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            Shippings.Create(null!, Money.FromDecimal(1_000m)));
    }

    [Fact]
    public void Create_WithNullBaseCost_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            Shippings.Create(ShippingName.Create("Post"), null!));
    }

    [Fact]
    public void Create_WithInvalidDeliveryRange_PropagatesDomainException()
    {
        Should.Throw<DomainException>(() => new ShippingBuilder().WithDeliveryDays(5, 4).Build());
    }

    [Fact]
    public void Update_WithChangedName_AssignsNewNameAndSetsUpdatedAt()
    {
        var sut = new ShippingBuilder().WithName("Post").WithBaseCost(50_000m).ClearEventsAfterBuild().Build();

        sut.Update(ShippingName.Create("Tipax"), Money.FromDecimal(50_000m), null, null, 1, 3);

        sut.Name.Value.ShouldBe("Tipax");
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Update_WithSameCost_RaisesOnlyShippingUpdatedEvent()
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).ClearEventsAfterBuild().Build();
        var versionBefore = sut.Version;

        sut.Update(ShippingName.Create("NewName"), Money.FromDecimal(50_000m), null, null, 1, 3);

        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ShippingUpdatedEvent>()
            .ShippingId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Update_WithChangedCost_RaisesShippingUpdatedAndShippingCostChangedEvents()
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).ClearEventsAfterBuild().Build();
        var versionBefore = sut.Version;

        sut.Update(ShippingName.Create("Post"), Money.FromDecimal(75_000m), null, null, 1, 3);

        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<ShippingUpdatedEvent>();
        var costEvt = sut.DomainEvents.ElementAt(1).ShouldBeOfType<ShippingCostChangedEvent>();
        costEvt.PreviousCost.ShouldBe(50_000m);
        costEvt.NewCost.ShouldBe(75_000m);
        costEvt.ShippingId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Update_TrimsDescriptionAndEstimatedDeliveryTime()
    {
        var sut = new ShippingBuilder().ClearEventsAfterBuild().Build();

        sut.Update(ShippingName.Create("Post"), Money.FromDecimal(1_000m), "  d  ", "  e  ", 1, 3);

        sut.Description.ShouldBe("d");
        sut.EstimatedDeliveryTime.ShouldBe("e");
    }

    [Fact]
    public void Update_WithNullName_ThrowsArgumentNullException()
    {
        var sut = new ShippingBuilder().Build();

        Should.Throw<ArgumentNullException>(() =>
            sut.Update(null!, Money.FromDecimal(1m), null, null, 1, 2));
    }

    [Fact]
    public void Update_WithNullBaseCost_ThrowsArgumentNullException()
    {
        var sut = new ShippingBuilder().Build();

        Should.Throw<ArgumentNullException>(() =>
            sut.Update(ShippingName.Create("Post"), null!, null, null, 1, 2));
    }

    [Fact]
    public void Update_WithInvalidDeliveryRange_PropagatesDomainException()
    {
        var sut = new ShippingBuilder().Build();

        Should.Throw<DomainException>(() =>
            sut.Update(ShippingName.Create("Post"), Money.FromDecimal(1m), null, null, 5, 4));
    }

    [Fact]
    public void CalculateCost_WhenInactive_ReturnsZero()
    {
        var sut = new ShippingBuilder().AsDeleted().Build();

        sut.CalculateCost(Money.FromDecimal(1_000_000m)).Amount.ShouldBe(0m);
    }

    [Fact]
    public void CalculateCost_WithDefaultMultiplier_ReturnsBaseCostRounded()
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).Build();

        sut.CalculateCost(Money.FromDecimal(100_000m)).Amount.ShouldBe(50_000m);
    }

    [Theory]
    [InlineData(1.0, 50_000)]
    [InlineData(2.0, 100_000)]
    [InlineData(1.5, 75_000)]
    public void CalculateCost_WithPositiveMultiplier_MultipliesBaseCost(decimal multiplier, decimal expected)
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).Build();

        sut.CalculateCost(Money.FromDecimal(0m), multiplier).Amount.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void CalculateCost_WithNonPositiveMultiplier_FallsBackToMultiplierOne(decimal multiplier)
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).Build();

        sut.CalculateCost(Money.FromDecimal(0m), multiplier).Amount.ShouldBe(50_000m);
    }

    [Fact]
    public void CalculateCost_RoundsResultToZeroDecimals()
    {
        var sut = new ShippingBuilder().WithBaseCost(33_333m).Build();

        sut.CalculateCost(Money.FromDecimal(0m), 1.5m).Amount.ShouldBe(Math.Round(33_333m * 1.5m, 0));
    }

    [Fact]
    public void CalculateCostForCart_WhenInactive_ReturnsZero()
    {
        var sut = new ShippingBuilder().AsDeleted().Build();

        sut.CalculateCostForCart(Money.FromDecimal(1_000_000m), []).Amount.ShouldBe(0m);
    }

    [Fact]
    public void CalculateCostForCart_WithEmptyItems_ReturnsBaseCostRounded()
    {
        var sut = new ShippingBuilder().WithBaseCost(40_000m).Build();

        sut.CalculateCostForCart(Money.FromDecimal(0m), []).Amount.ShouldBe(40_000m);
    }

    [Fact]
    public void CalculateCostForCart_SumsMultiplierTimesQuantityAcrossItems()
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).Build();
        var items = new[]
        {
            new ShippingCostItem(VariantId.NewId(), 1m, 2),
            new ShippingCostItem(VariantId.NewId(), 1.5m, 1)
        };

        sut.CalculateCostForCart(Money.FromDecimal(0m), items).Amount.ShouldBe(50_000m * (1m * 2 + 1.5m * 1));
    }

    [Fact]
    public void CalculateCostForCart_SkipsItemsWithZeroOrNegativeQuantity()
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).Build();
        var items = new[]
        {
            new ShippingCostItem(VariantId.NewId(), 2m, 0),
            new ShippingCostItem(VariantId.NewId(), 3m, -5),
            new ShippingCostItem(VariantId.NewId(), 1m, 4)
        };

        sut.CalculateCostForCart(Money.FromDecimal(0m), items).Amount.ShouldBe(50_000m * 4m);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    public void CalculateCostForCart_ReplacesNonPositiveMultiplierWithOne(decimal multiplier)
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).Build();
        var items = new[] { new ShippingCostItem(VariantId.NewId(), multiplier, 3) };

        sut.CalculateCostForCart(Money.FromDecimal(0m), items).Amount.ShouldBe(50_000m * 3m);
    }

    [Fact]
    public void CalculateCostForCart_WhenAllQuantitiesAreZero_ReturnsBaseCostRounded()
    {
        var sut = new ShippingBuilder().WithBaseCost(40_000m).Build();
        var items = new[]
        {
            new ShippingCostItem(VariantId.NewId(), 2m, 0),
            new ShippingCostItem(VariantId.NewId(), 3m, 0)
        };

        sut.CalculateCostForCart(Money.FromDecimal(0m), items).Amount.ShouldBe(40_000m);
    }

    [Fact]
    public void IsAvailableForOrder_WhenInactive_ReturnsFalse()
    {
        var sut = new ShippingBuilder().AsDeleted().Build();

        sut.IsAvailableForOrder(Money.FromDecimal(1_000_000m)).ShouldBeFalse();
    }

    [Fact]
    public void IsAvailableForOrder_WhenActiveWithUnlimitedRange_ReturnsTrue()
    {
        var sut = new ShippingBuilder().Build();

        sut.IsAvailableForOrder(Money.FromDecimal(1m)).ShouldBeTrue();
    }

    [Fact]
    public void ValidateForOrder_WhenInactive_ReturnsFailureWithCode400()
    {
        var sut = new ShippingBuilder().AsDeleted().Build();

        sut.ValidateForOrder(Money.FromDecimal(1_000_000m))
            .ShouldFailWith("400");
    }

    [Fact]
    public void ValidateForOrder_WhenActiveWithUnlimitedRange_ReturnsSuccess()
    {
        var sut = new ShippingBuilder().Build();

        sut.ValidateForOrder(Money.FromDecimal(1_000m)).ShouldBeSuccess();
    }

    [Fact]
    public void SetAsDefault_OnActiveShipping_MarksAsDefaultAndRaisesEvent()
    {
        var sut = new ShippingBuilder().ClearEventsAfterBuild().Build();
        var versionBefore = sut.Version;

        sut.SetAsDefault();

        sut.IsDefault.ShouldBeTrue();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<ShippingSetAsDefaultEvent>();
        evt.ShippingId.ShouldBe(sut.Id);
        evt.Name.ShouldBe(sut.Name);
    }

    [Fact]
    public void SetAsDefault_OnInactiveShipping_ThrowsDomainException()
    {
        var sut = new ShippingBuilder().AsDeleted().Build();

        Should.Throw<DomainException>(sut.SetAsDefault);
    }

    [Fact]
    public void UnsetDefault_OnDefaultShipping_ClearsDefaultFlagWithoutRaisingEvent()
    {
        var sut = new ShippingBuilder().AsDefault().ClearEventsAfterBuild().Build();
        var versionBefore = sut.Version;

        sut.UnsetDefault();

        sut.IsDefault.ShouldBeFalse();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UnsetDefault_OnNonDefaultShipping_IsIdempotent()
    {
        var sut = new ShippingBuilder().ClearEventsAfterBuild().Build();

        Should.NotThrow(sut.UnsetDefault);
        sut.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void RequestDeletion_OnNonDefaultShipping_DeactivatesAndRaisesEvent()
    {
        var deletedBy = UserId.NewId();
        var sut = new ShippingBuilder().ClearEventsAfterBuild().Build();
        var versionBefore = sut.Version;

        sut.RequestDeletion(deletedBy);

        sut.IsActive.ShouldBeFalse();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<ShippingDeletedEvent>();
        evt.ShippingId.ShouldBe(sut.Id);
        evt.DeletedBy.ShouldBe(deletedBy);
    }

    [Fact]
    public void RequestDeletion_WithoutDeletedBy_RaisesEventWithNullDeletedBy()
    {
        var sut = new ShippingBuilder().ClearEventsAfterBuild().Build();

        sut.RequestDeletion();

        sut.DomainEvents.Single().ShouldBeOfType<ShippingDeletedEvent>().DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void RequestDeletion_OnDefaultShipping_ThrowsDefaultShippingCannotBeDeletedException()
    {
        var sut = new ShippingBuilder().AsDefault().Build();

        var ex = Should.Throw<DefaultShippingCannotBeDeletedException>(() => sut.RequestDeletion());
        ex.ShippingId.ShouldBe(sut.Id);
        ex.ErrorCode.ShouldBe("DEFAULT_SHIPPING_CANNOT_BE_DELETED");
    }

    [Fact]
    public void Restore_OnInactiveShipping_ReactivatesAndUpdatesTimestamp()
    {
        var sut = new ShippingBuilder().AsDeleted().ClearEventsAfterBuild().Build();

        sut.Restore();

        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Restore_OnActiveShipping_IsNoOpAndRaisesNoEvent()
    {
        var sut = new ShippingBuilder().ClearEventsAfterBuild().Build();
        var updatedAtBefore = sut.UpdatedAt;

        sut.Restore();

        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Restore_OnInactiveShipping_DoesNotRaiseAnyDomainEvent()
    {
        var sut = new ShippingBuilder().AsDeleted().ClearEventsAfterBuild().Build();

        sut.Restore();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void GetDeliveryTimeDisplay_WithoutCustomLabel_ReturnsRangeFormat()
    {
        var sut = new ShippingBuilder().WithDeliveryDays(2, 5).WithEstimatedDeliveryTime(null).Build();

        sut.GetDeliveryTimeDisplay().ShouldBe("2 تا 5 روز کاری");
    }

    [Fact]
    public void GetDeliveryTimeDisplay_WithCustomLabel_ReturnsCustomLabel()
    {
        var sut = new ShippingBuilder().WithDeliveryDays(2, 5).WithEstimatedDeliveryTime("امروز").Build();

        sut.GetDeliveryTimeDisplay().ShouldBe("امروز");
    }

    [Fact]
    public void QualifiesForFreeShipping_ForShippingCreatedThroughFactory_ReturnsFalse()
    {
        var sut = new ShippingBuilder().Build();

        sut.QualifiesForFreeShipping(Money.FromDecimal(999_999_999m)).ShouldBeFalse();
    }

    [Fact]
    public void CostProperty_MirrorsBaseCost()
    {
        var sut = new ShippingBuilder().WithBaseCost(123_456m).Build();

        sut.Cost.ShouldBe(sut.BaseCost);
    }

    [Fact]
    public void LifecycleSequence_VersionGrowsByOnePerRaisedEvent()
    {
        var sut = new ShippingBuilder().WithBaseCost(50_000m).Build();
        sut.Version.ShouldBe(1);

        sut.Update(ShippingName.Create("Post"), Money.FromDecimal(50_000m), null, null, 1, 3);
        sut.Version.ShouldBe(2);

        sut.Update(ShippingName.Create("Post"), Money.FromDecimal(75_000m), null, null, 1, 3);
        sut.Version.ShouldBe(4);

        sut.SetAsDefault();
        sut.Version.ShouldBe(5);

        sut.UnsetDefault();
        sut.Version.ShouldBe(5);

        sut.RequestDeletion();
        sut.Version.ShouldBe(6);
    }
}
