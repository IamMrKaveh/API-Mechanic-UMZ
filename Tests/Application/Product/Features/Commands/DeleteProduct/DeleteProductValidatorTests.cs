using Application.Product.Features.Commands.DeleteProduct;

namespace Tests.Application.Product.Features.Commands.DeleteProduct;

public class DeleteProductValidatorTests
{
    private readonly DeleteProductValidator _sut = new();

    [Fact]
    public void Validate_WithValidProductId_IsValid()
    {
        var command = new DeleteProductCommand(Guid.NewGuid());

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyProductId_FailsOnProductId()
    {
        var command = new DeleteProductCommand(Guid.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DeleteProductCommand.ProductId));
    }
}
