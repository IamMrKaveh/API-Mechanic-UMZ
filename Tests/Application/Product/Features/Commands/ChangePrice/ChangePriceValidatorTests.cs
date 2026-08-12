using Application.Product.Features.Commands.ChangePrice;

namespace Tests.Application.Product.Features.Commands.ChangePrice;

public class ChangePriceValidatorTests
{
    private readonly ChangePriceValidator _sut = new();

    private static ChangePriceCommand ValidCommand(
        Guid? productId = null,
        Guid? variantId = null,
        Guid? userId = null,
        decimal sellingPrice = 100_000m,
        decimal originalPrice = 120_000m)
        => new(
            productId ?? Guid.NewGuid(),
            variantId ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            sellingPrice,
            originalPrice);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyProductId_FailsOnProductId()
    {
        var result = _sut.Validate(ValidCommand(productId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePriceCommand.ProductId));
    }

    [Fact]
    public void Validate_WithEmptyVariantId_FailsOnVariantId()
    {
        var result = _sut.Validate(ValidCommand(variantId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePriceCommand.VariantId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveSellingPrice_FailsOnSellingPrice(decimal sellingPrice)
    {
        var result = _sut.Validate(ValidCommand(sellingPrice: sellingPrice));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePriceCommand.SellingPrice));
    }

    [Fact]
    public void Validate_WithNegativeOriginalPrice_FailsOnOriginalPrice()
    {
        var result = _sut.Validate(ValidCommand(originalPrice: -1m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePriceCommand.OriginalPrice));
    }

    [Fact]
    public void Validate_WithZeroOriginalPrice_DoesNotFailOnOriginalPrice()
    {
        var result = _sut.Validate(ValidCommand(originalPrice: 0m));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(ChangePriceCommand.OriginalPrice));
    }
}
