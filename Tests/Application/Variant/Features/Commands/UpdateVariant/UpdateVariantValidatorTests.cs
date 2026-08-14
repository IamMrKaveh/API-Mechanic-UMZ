using Application.Variant.Features.Commands.UpdateVariant;

namespace Tests.Application.Variant.Features.Commands.UpdateVariant;

public class UpdateVariantValidatorTests { private readonly UpdateVariantValidator _sut = new();

private static UpdateVariantCommand ValidCommand(
    Guid? productId = null,
    Guid? variantId = null,
    string? sku = "SKU-001",
    decimal sellingPrice = 100_000m,
    decimal originalPrice = 0m,
    int stock = 5,
    bool isUnlimited = false,
    decimal shippingMultiplier = 1m,
    ICollection<Guid>? attributeValueIds = null,
    ICollection<Guid>? enabledShippingIds = null)
{
    return new UpdateVariantCommand(
        productId ?? Guid.NewGuid(),
        variantId ?? Guid.NewGuid(),
        sku,
        sellingPrice,
        originalPrice,
        stock,
        isUnlimited,
        shippingMultiplier,
        attributeValueIds,
        enabledShippingIds);
}

[Fact]
public void Validate_WithValidCommand_IsValid()
{
    var result = _sut.Validate(ValidCommand());

    result.IsValid.ShouldBeTrue();
}

[Fact]
public void Validate_WithEmptyProductId_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(productId: Guid.Empty));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantCommand.ProductId));
}

[Fact]
public void Validate_WithEmptyVariantId_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(variantId: Guid.Empty));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantCommand.VariantId));
}

[Theory]
[InlineData(0)]
[InlineData(-100)]
public void Validate_WithNonPositiveSellingPrice_IsInvalid(decimal sellingPrice)
{
    var result = _sut.Validate(ValidCommand(sellingPrice: sellingPrice));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantCommand.SellingPrice));
}

[Fact]
public void Validate_WithNegativeOriginalPrice_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(originalPrice: -1m));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantCommand.OriginalPrice));
}

[Fact]
public void Validate_WithNegativeStockAndNotUnlimited_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(stock: -1, isUnlimited: false));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantCommand.Stock));
}

[Fact]
public void Validate_WithNegativeStockButUnlimited_IsValid()
{
    var result = _sut.Validate(ValidCommand(stock: -1, isUnlimited: true));

    result.IsValid.ShouldBeTrue();
}

[Theory]
[InlineData(0.05)]
[InlineData(10.1)]
public void Validate_WithShippingMultiplierOutOfRange_IsInvalid(decimal multiplier)
{
    var result = _sut.Validate(ValidCommand(shippingMultiplier: multiplier));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantCommand.ShippingMultiplier));
}
}