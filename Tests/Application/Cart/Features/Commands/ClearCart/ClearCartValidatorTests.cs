using Application.Cart.Features.Commands.ClearCart;

namespace Tests.Application.Cart.Features.Commands.ClearCart;

public class ClearCartValidatorTests
{
    private readonly ClearCartValidator _sut = new();

    [Fact]
    public void Validate_WhenCommandIsDefault_IsValid()
    {
        var command = new ClearCartCommand();

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenCommandIsDefault_HasNoErrors()
    {
        var command = new ClearCartCommand();

        var result = _sut.Validate(command);

        result.Errors.ShouldBeEmpty();
    }
}
