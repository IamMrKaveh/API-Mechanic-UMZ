using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Inventory.Services;

public class InventoryReservationServiceTests
{
    [Fact]
    public void ValidateBatchAvailability_WithEmptyList_ReturnsValid()
    {
        var sut = new InventoryReservationService();

        var result = sut.ValidateBatchAvailability(Array.Empty<BatchReservationItem>());

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateBatchAvailability_WithAllPositiveQuantities_ReturnsValid()
    {
        var sut = new InventoryReservationService();
        var items = new[]
        {
            new BatchReservationItemBuilder().WithQuantity(1).Build(),
            new BatchReservationItemBuilder().WithQuantity(5).Build()
        };

        var result = sut.ValidateBatchAvailability(items);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateBatchAvailability_WithZeroQuantityItem_ReturnsInvalidWithErrorMentioningVariant()
    {
        var variantId = VariantId.NewId();
        var sut = new InventoryReservationService();
        var items = new[]
        {
            new BatchReservationItemBuilder().WithVariantId(variantId).WithQuantity(0).Build()
        };

        var result = sut.ValidateBatchAvailability(items);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.Single().ShouldContain(variantId.ToString());
    }

    [Fact]
    public void ValidateBatchAvailability_WithMultipleZeroQuantityItems_ReturnsOneErrorPerInvalidItem()
    {
        var sut = new InventoryReservationService();
        var items = new[]
        {
            new BatchReservationItemBuilder().WithQuantity(0).Build(),
            new BatchReservationItemBuilder().WithQuantity(5).Build(),
            new BatchReservationItemBuilder().WithQuantity(0).Build()
        };

        var result = sut.ValidateBatchAvailability(items);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public void ReserveBatch_WithAllValidItems_ReturnsSuccessWithNoErrors()
    {
        var sut = new InventoryReservationService();
        var items = new[] { new BatchReservationItemBuilder().WithQuantity(1).Build() };

        var result = sut.ReserveBatch(items);

        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ReserveBatch_WhenAnyItemInvalid_ReturnsFailureCarryingValidationErrors()
    {
        var sut = new InventoryReservationService();
        var items = new[] { new BatchReservationItemBuilder().WithQuantity(0).Build() };

        var result = sut.ReserveBatch(items);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void ReleaseBatch_WithEmptyList_ReturnsFailure()
    {
        var sut = new InventoryReservationService();

        var result = sut.ReleaseBatch(Array.Empty<BatchReservationItem>());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("No items provided for release.");
    }

    [Fact]
    public void ReleaseBatch_WithAtLeastOneItem_ReturnsSuccess()
    {
        var sut = new InventoryReservationService();
        var items = new[] { new BatchReservationItemBuilder().Build() };

        var result = sut.ReleaseBatch(items);

        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }
}
