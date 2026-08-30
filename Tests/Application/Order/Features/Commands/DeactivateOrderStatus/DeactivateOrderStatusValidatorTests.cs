using Application.Order.Features.Commands.DeactivateOrderStatus;

namespace Tests.Application.Order.Features.Commands.DeactivateOrderStatus;

public class DeactivateOrderStatusValidatorTests
{
    private readonly DeactivateOrderStatusValidator _sut = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_HasErrorForId()
    {
        var result = _sut.Validate(new DeactivateOrderStatusCommand(Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(DeactivateOrderStatusCommand.Id) &&
            e.ErrorMessage == "شناسه وضعیت الزامی است.");
    }

    [Fact]
    public void Validate_WhenIdIsNotEmpty_IsValid()
    {
        var result = _sut.Validate(new DeactivateOrderStatusCommand(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
