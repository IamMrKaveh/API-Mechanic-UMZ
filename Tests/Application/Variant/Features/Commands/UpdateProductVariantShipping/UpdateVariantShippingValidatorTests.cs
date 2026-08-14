using Application.Variant.Features.Commands.UpdateProductVariantShipping;

namespace Tests.Application.Variant.Features.Commands.UpdateProductVariantShipping;

public class UpdateVariantShippingValidatorTests { private readonly UpdateVariantShippingValidator _sut = new();

private static UpdateVariantShippingCommand ValidCommand(
    Guid? variantId = null,
    decimal shippingMultiplier = 1m,
    decimal weightGrams = 100m,
    ICollection<Guid>? enabledShippingIds = null)
{
    return new UpdateVariantShippingCommand(
        variantId ?? Guid.NewGuid(),
        shippingMultiplier,
        weightGrams,
        enabledShippingIds ?? Array.Empty<Guid>());
}

[Fact]
public void Validate_WithValidCommand_IsValid()
{
    var result = _sut.Validate(ValidCommand());

    result.IsValid.ShouldBeTrue();
}

[Fact]
public void Validate_WithEmptyVariantId_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(variantId: Guid.Empty));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantShippingCommand.VariantId));
}

[Theory]
[InlineData(0.05)]
[InlineData(0)]
[InlineData(100.1)]
public void Validate_WithShippingMultiplierOutOfRange_IsInvalid(decimal multiplier)
{
    var result = _sut.Validate(ValidCommand(shippingMultiplier: multiplier));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantShippingCommand.ShippingMultiplier));
}

[Fact]
public void Validate_WithNegativeWeight_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(weightGrams: -1m));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantShippingCommand.WeightGrams));
}

[Fact]
public void Validate_WithWeightExceedingMaximum_IsInvalid()
{
    var result = _sut.Validate(ValidCommand(weightGrams: 500_001m));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantShippingCommand.WeightGrams));
}

[Fact]
public void Validate_WithNullEnabledShippingIds_IsInvalid()
{
    var command = new UpdateVariantShippingCommand(Guid.NewGuid(), 1m, 100m, null!);

    var result = _sut.Validate(command);

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateVariantShippingCommand.EnabledShippingIds));
}
}