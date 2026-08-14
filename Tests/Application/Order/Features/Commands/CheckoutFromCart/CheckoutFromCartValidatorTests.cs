using Application.Order.Features.Commands.CheckoutFromCart;

namespace Tests.Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartValidatorTests { private readonly CheckoutFromCartValidator _sut = new();

private static CheckoutFromCartCommand ValidCommand() =>
    new(
        CartId: Guid.NewGuid(),
        ShippingId: Guid.NewGuid(),
        AddressId: Guid.NewGuid(),
        DiscountCode: null,
        PaymentMethod: null,
        PaymentMethodId: null,
        IdempotencyKey: Guid.NewGuid());

[Fact]
public void Validate_WhenAllFieldsValid_IsValid()
{
    _sut.Validate(ValidCommand()).IsValid.ShouldBeTrue();
}

[Theory]
[InlineData(nameof(CheckoutFromCartCommand.CartId))]
[InlineData(nameof(CheckoutFromCartCommand.ShippingId))]
[InlineData(nameof(CheckoutFromCartCommand.AddressId))]
[InlineData(nameof(CheckoutFromCartCommand.IdempotencyKey))]
public void Validate_WhenRequiredGuidEmpty_HasErrorForThatField(string field)
{
    var cmd = ValidCommand() switch
    {
        var c when field == nameof(CheckoutFromCartCommand.CartId) => c with { CartId = Guid.Empty },
        var c when field == nameof(CheckoutFromCartCommand.ShippingId) => c with { ShippingId = Guid.Empty },
        var c when field == nameof(CheckoutFromCartCommand.AddressId) => c with { AddressId = Guid.Empty },
        var c when field == nameof(CheckoutFromCartCommand.IdempotencyKey) => c with { IdempotencyKey = Guid.Empty },
        _ => ValidCommand()
    };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == field);
}

[Fact]
public void Validate_WhenDiscountCodeExceeds50Chars_HasErrorForDiscountCode()
{
    var cmd = ValidCommand() with { DiscountCode = new string('X', 51) };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CheckoutFromCartCommand.DiscountCode));
}

[Fact]
public void Validate_WhenDiscountCodeNull_HasNoErrorForDiscountCode()
{
    var cmd = ValidCommand() with { DiscountCode = null };

    var result = _sut.Validate(cmd);
    result.Errors.ShouldNotContain(e => e.PropertyName == nameof(CheckoutFromCartCommand.DiscountCode));
}
}