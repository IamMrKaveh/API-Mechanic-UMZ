using Application.Order.Features.Commands.UpdateOrderStatus;

namespace Tests.Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusValidatorTests { private readonly UpdateOrderStatusValidator _sut = new();

[Fact]
public void Validate_WhenOrderIdEmpty_HasErrorForOrderId()
{
    var result = _sut.Validate(new UpdateOrderStatusCommand(Guid.Empty, "Paid", "AA=="));

    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateOrderStatusCommand.OrderId));
}

[Fact]
public void Validate_WhenNewStatusEmpty_HasErrorForNewStatus()
{
    var result = _sut.Validate(new UpdateOrderStatusCommand(Guid.NewGuid(), string.Empty, "AA=="));

    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateOrderStatusCommand.NewStatus));
}

[Fact]
public void Validate_WhenRowVersionEmpty_HasErrorForRowVersion()
{
    var result = _sut.Validate(new UpdateOrderStatusCommand(Guid.NewGuid(), "Paid", string.Empty));

    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateOrderStatusCommand.RowVersion));
}

[Fact]
public void Validate_WhenAllFieldsValid_IsValid()
{
    _sut.Validate(new UpdateOrderStatusCommand(Guid.NewGuid(), "Paid", "AA==")).IsValid.ShouldBeTrue();
}
}