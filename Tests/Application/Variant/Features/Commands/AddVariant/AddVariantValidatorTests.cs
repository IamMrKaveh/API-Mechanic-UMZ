using Application.Variant.Features.Commands.AddVariant;

namespace Tests.Application.Variant.Features.Commands.AddVariant;

public class AddVariantValidatorTests { private readonly AddVariantValidator _sut = new();

private static AddVariantCommand ValidCommand(
    Guid? productId = null,
    string? sku = "SKU-001",
    decimal sellingPrice = 100_000m,
    decimal originalPrice = 0m,
    int stock = 5,
    bool isUnlimited = false,
    decimal shippingMultiplier = 1m,
    ICollection<Guid>? attributeValueIds = null,
    ICollection<Guid>? enabledShippingIds = null)
{
    return new AddVariantCommand(
        productId ?? Guid.NewGuid(),
        sku,
        sellingPrice,
        originalPrice,
        stock,
        isUnlimited,
        shippingMultiplier,
        attributeValueIds ?? Array.Empty<Guid>(),
        enabledShippingIds ?? Array.Empty<Guid>());
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
    result.Errors.ShouldContain(e => e.PropertyName == nameof(AddVariantCommand.ProductId));
}

[Theory]
[InlineData(0)]
[InlineData(-1)]
public void Validate_WithNonPositiveSellingPrice_IsInvalid(decimal sellingPrice)
{
    var result = _sut.Validate(ValidCommand(sellingPrice: sellingPrice));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(AddVariantCommand.SellingPrice));
}

[Fact]
public void Validate_WithNegativeOriginalPrice_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(originalPrice: -1m));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(AddVariantCommand.OriginalPrice));
}

[Fact]
public void Validate_WithNegativeStockAndNotUnlimited_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(stock: -1, isUnlimited: false));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(AddVariantCommand.Stock));
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
    result.Errors.ShouldContain(e => e.PropertyName == nameof(AddVariantCommand.ShippingMultiplier));
}

[Fact]
public void Validate_WithOriginalPricePositiveAndLessThanSellingPrice_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(sellingPrice: 100_000m, originalPrice: 50_000m));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(AddVariantCommand.OriginalPrice));
}

[Fact]
public void Validate_WithOriginalPriceGreaterThanSellingPrice_IsValid()
{
    var result = _sut.Validate(ValidCommand(sellingPrice: 100_000m, originalPrice: 150_000m));

    result.IsValid.ShouldBeTrue();
}
}