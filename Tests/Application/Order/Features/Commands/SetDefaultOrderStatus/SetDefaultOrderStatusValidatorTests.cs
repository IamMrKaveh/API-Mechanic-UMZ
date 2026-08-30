using Application.Order.Features.Commands.SetDefaultOrderStatus;

namespace Tests.Application.Order.Features.Commands.SetDefaultOrderStatus;

public class SetDefaultOrderStatusValidatorTests
{
    private readonly SetDefaultOrderStatusValidator _sut = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_HasErrorForId()
    {
        var result = _sut.Validate(new SetDefaultOrderStatusCommand(Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetDefaultOrderStatusCommand.Id) &&
            e.ErrorMessage == "شناسه وضعیت الزامی است.");
    }

    [Fact]
    public void Validate_WhenIdIsNotEmpty_IsValid()
    {
        var result = _sut.Validate(new SetDefaultOrderStatusCommand(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
