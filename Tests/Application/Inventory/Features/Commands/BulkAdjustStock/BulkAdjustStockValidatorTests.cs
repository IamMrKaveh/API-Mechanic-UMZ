using Application.Inventory.Features.Commands.BulkAdjustStock;

namespace Tests.Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockValidatorTests
{
    private readonly BulkAdjustStockValidator _sut = new();

    [Fact]
    public void Validate_WithValidItemsAndReason_ReturnsIsValidTrue()
    {
        var command = new BulkAdjustStockCommand(
            new[] { new BulkAdjustStockItem(Guid.NewGuid(), 5) },
            "restock");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyItemList_ReturnsError()
    {
        var command = new BulkAdjustStockCommand(Array.Empty<BulkAdjustStockItem>(), "restock");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(BulkAdjustStockCommand.Items));
    }

    [Fact]
    public void Validate_WithEmptyReason_ReturnsError()
    {
        var command = new BulkAdjustStockCommand(
            new[] { new BulkAdjustStockItem(Guid.NewGuid(), 1) },
            string.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(BulkAdjustStockCommand.Reason));
    }

    [Fact]
    public void Validate_WithChildItemHavingZeroChange_ReturnsError()
    {
        var command = new BulkAdjustStockCommand(
            new[] { new BulkAdjustStockItem(Guid.NewGuid(), 0) },
            "restock");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains(nameof(BulkAdjustStockItem.QuantityChange)));
    }

    [Fact]
    public void Validate_WithChildItemHavingEmptyVariantId_ReturnsError()
    {
        var command = new BulkAdjustStockCommand(
            new[] { new BulkAdjustStockItem(Guid.Empty, 1) },
            "restock");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains(nameof(BulkAdjustStockItem.VariantId)));
    }
}
