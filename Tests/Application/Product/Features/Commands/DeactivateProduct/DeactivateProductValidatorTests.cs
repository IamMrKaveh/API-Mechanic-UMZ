using Application.Product.Features.Commands.DeactivateProduct;

namespace Tests.Application.Product.Features.Commands.DeactivateProduct;

public class DeactivateProductValidatorTests
{
    private readonly DeactivateProductValidator _sut = new();

    [Fact]
    public void Validate_WithValidProductId_IsValid()
    {
        var command = new DeactivateProductCommand(Guid.NewGuid());

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyProductId_FailsOnProductId()
    {
        var command = new DeactivateProductCommand(Guid.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DeactivateProductCommand.ProductId));
    }
}
