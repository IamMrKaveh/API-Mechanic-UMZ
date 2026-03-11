namespace Tests.ApplicationTest.Cart;

public class UpdateCartItemQuantityValidatorTests
{
    private readonly UpdateCartItemQuantityValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        var command = new UpdateCartItemQuantityCommand(VariantId: 1, Quantity: 3);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithZeroVariantId_ShouldHaveError()
    {
        var command = new UpdateCartItemQuantityCommand(VariantId: 0, Quantity: 3);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.VariantId);
    }

    [Fact]
    public void Validate_WithNegativeVariantId_ShouldHaveError()
    {
        var command = new UpdateCartItemQuantityCommand(VariantId: -1, Quantity: 3);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.VariantId);
    }

    [Fact]
    public void Validate_WithNegativeQuantity_ShouldHaveError()
    {
        var command = new UpdateCartItemQuantityCommand(VariantId: 1, Quantity: -1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithZeroQuantity_ShouldNotHaveError()
    {
        var command = new UpdateCartItemQuantityCommand(VariantId: 1, Quantity: 0);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithQuantityAbove1000_ShouldHaveError()
    {
        var command = new UpdateCartItemQuantityCommand(VariantId: 1, Quantity: 1001);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithMaxAllowedQuantity_ShouldNotHaveError()
    {
        var command = new UpdateCartItemQuantityCommand(VariantId: 1, Quantity: 1000);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }
}