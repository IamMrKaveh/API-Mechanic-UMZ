using Application.Product.Features.Commands.ActivateProduct;

namespace Tests.Application.Product.Features.Commands.ActivateProduct;

public class ActivateProductValidatorTests
{
    private readonly ActivateProductValidator _sut = new();

    [Fact]
    public void Validate_WithValidProductId_IsValid()
    {
        var command = new ActivateProductCommand(Guid.NewGuid());

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyProductId_FailsOnProductId()
    {
        var command = new ActivateProductCommand(Guid.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ActivateProductCommand.ProductId));
    }
}
