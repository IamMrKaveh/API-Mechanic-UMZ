using Application.Variant.Features.Commands.RemoveVariant;

namespace Tests.Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantValidatorTests { private readonly RemoveVariantValidator _sut = new();

[Fact]
public void Validate_WithValidIds_IsValid()
{
    var result = _sut.Validate(new RemoveVariantCommand(Guid.NewGuid(), Guid.NewGuid()));

    result.IsValid.ShouldBeTrue();
}

[Fact]
public void Validate_WithEmptyProductId_IsInvalid()
{
    var result = _sut.Validate(new RemoveVariantCommand(Guid.Empty, Guid.NewGuid()));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(RemoveVariantCommand.ProductId));
}

[Fact]
public void Validate_WithEmptyVariantId_IsInvalid()
{
    var result = _sut.Validate(new RemoveVariantCommand(Guid.NewGuid(), Guid.Empty));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(RemoveVariantCommand.VariantId));
}

[Fact]
public void Validate_WithBothIdsEmpty_HasErrorsForBothProperties()
{
    var result = _sut.Validate(new RemoveVariantCommand(Guid.Empty, Guid.Empty));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(RemoveVariantCommand.ProductId));
    result.Errors.ShouldContain(e => e.PropertyName == nameof(RemoveVariantCommand.VariantId));
}
}