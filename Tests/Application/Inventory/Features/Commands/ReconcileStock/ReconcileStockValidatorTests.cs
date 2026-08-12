using Application.Inventory.Features.Commands.ReconcileStock;

namespace Tests.Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockValidatorTests
{
    private readonly ReconcileStockValidator _sut = new();

    [Fact]
    public void Validate_WithValidValues_ReturnsIsValidTrue()
    {
        var command = new ReconcileStockCommand(Guid.NewGuid(), 10);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyVariantId_ReturnsError()
    {
        var command = new ReconcileStockCommand(Guid.Empty, 0);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReconcileStockCommand.VariantId));
    }

    [Fact]
    public void Validate_WithNegativeCalculatedStock_ReturnsError()
    {
        var command = new ReconcileStockCommand(Guid.NewGuid(), -1);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReconcileStockCommand.CalculatedStock));
    }
}
