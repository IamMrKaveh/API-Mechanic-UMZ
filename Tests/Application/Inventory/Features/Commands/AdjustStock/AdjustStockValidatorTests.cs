using Application.Inventory.Features.Commands.AdjustStock;

namespace Tests.Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockValidatorTests
{
    private readonly AdjustStockValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsIsValidTrue()
    {
        var command = new AdjustStockCommand(Guid.NewGuid(), 3, "reason");

        ValidationResult result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyVariantId_ReturnsError()
    {
        var command = new AdjustStockCommand(Guid.Empty, 3, "reason");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(AdjustStockCommand.VariantId));
    }

    [Fact]
    public void Validate_WithZeroQuantityChange_ReturnsError()
    {
        var command = new AdjustStockCommand(Guid.NewGuid(), 0, "reason");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(AdjustStockCommand.QuantityChange));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyReason_ReturnsError(string? reason)
    {
        var command = new AdjustStockCommand(Guid.NewGuid(), 3, reason!);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(AdjustStockCommand.Reason));
    }
}
