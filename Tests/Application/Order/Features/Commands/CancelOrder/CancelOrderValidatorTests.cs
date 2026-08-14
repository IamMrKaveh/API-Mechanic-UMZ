using Application.Order.Features.Commands.CancelOrder;

namespace Tests.Application.Order.Features.Commands.CancelOrder;

public class CancelOrderValidatorTests { private readonly CancelOrderValidator _sut = new();

[Fact]
public void Validate_WhenOrderIdIsEmpty_HasErrorForOrderId()
{
    var result = _sut.Validate(new CancelOrderCommand(Guid.Empty, "reason", null));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(CancelOrderCommand.OrderId));
}

[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void Validate_WhenReasonIsNullOrWhitespace_HasErrorForReason(string? reason)
{
    var result = _sut.Validate(new CancelOrderCommand(Guid.NewGuid(), reason!, null));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(CancelOrderCommand.Reason));
}

[Fact]
public void Validate_WhenReasonExceeds500Chars_HasErrorForReason()
{
    var reason = new string('a', 501);

    var result = _sut.Validate(new CancelOrderCommand(Guid.NewGuid(), reason, null));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(CancelOrderCommand.Reason));
}

[Fact]
public void Validate_WhenAllFieldsValid_IsValid()
{
    var result = _sut.Validate(new CancelOrderCommand(Guid.NewGuid(), "customer changed mind", null));

    result.IsValid.ShouldBeTrue();
}
}