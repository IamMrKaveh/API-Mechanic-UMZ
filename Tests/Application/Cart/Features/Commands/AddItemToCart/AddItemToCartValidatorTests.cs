using Application.Cart.Features.Commands.AddItemToCart;

namespace Tests.Application.Cart.Features.Commands.AddItemToCart;

public class AddItemToCartValidatorTests
{
    private readonly AddItemToCartValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new AddItemToCartCommand(Guid.NewGuid(), 1);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyVariantId_FailsOnVariantId()
    {
        var command = new AddItemToCartCommand(Guid.Empty, 1);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(AddItemToCartCommand.VariantId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveQuantity_FailsOnQuantity(int quantity)
    {
        var command = new AddItemToCartCommand(Guid.NewGuid(), quantity);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(AddItemToCartCommand.Quantity));
    }

    [Theory]
    [InlineData(101)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Validate_WithQuantityAboveMax_FailsOnQuantity(int quantity)
    {
        var command = new AddItemToCartCommand(Guid.NewGuid(), quantity);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(AddItemToCartCommand.Quantity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_WithQuantityWithinAllowedRange_IsValid(int quantity)
    {
        var command = new AddItemToCartCommand(Guid.NewGuid(), quantity);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
