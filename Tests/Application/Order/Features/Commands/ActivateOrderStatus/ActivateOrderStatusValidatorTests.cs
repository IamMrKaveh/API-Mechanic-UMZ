using Application.Order.Features.Commands.ActivateOrderStatus;

namespace Tests.Application.Order.Features.Commands.ActivateOrderStatus;

public class ActivateOrderStatusValidatorTests
{
    private readonly ActivateOrderStatusValidator _sut = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_HasErrorForId()
    {
        var result = _sut.Validate(new ActivateOrderStatusCommand(Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ActivateOrderStatusCommand.Id) &&
            e.ErrorMessage == "شناسه وضعیت الزامی است.");
    }

    [Fact]
    public void Validate_WhenIdIsNotEmpty_IsValid()
    {
        var result = _sut.Validate(new ActivateOrderStatusCommand(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
