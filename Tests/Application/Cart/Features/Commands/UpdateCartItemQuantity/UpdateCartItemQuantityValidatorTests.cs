using Application.Cart.Features.Commands.UpdateCartItemQuantity;

namespace Tests.Application.Cart.Features.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityValidatorTests
{
    private readonly UpdateCartItemQuantityValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), 5);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyVariantId_FailsOnVariantId()
    {
        var command = new UpdateCartItemQuantityCommand(Guid.Empty, 5);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCartItemQuantityCommand.VariantId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNegativeQuantity_FailsOnQuantity(int quantity)
    {
        var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), quantity);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCartItemQuantityCommand.Quantity));
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(5000)]
    public void Validate_WithQuantityAboveMax_FailsOnQuantity(int quantity)
    {
        var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), quantity);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCartItemQuantityCommand.Quantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Validate_WithQuantityWithinAllowedRange_IsValid(int quantity)
    {
        var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), quantity);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
