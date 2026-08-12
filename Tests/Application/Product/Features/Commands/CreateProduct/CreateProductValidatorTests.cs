using Application.Product.Features.Commands.CreateProduct;

namespace Tests.Application.Product.Features.Commands.CreateProduct;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new CreateProductCommand(Guid.NewGuid(), Guid.NewGuid(), "Sample Product");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_FailsOnName(string name)
    {
        var command = new CreateProductCommand(Guid.NewGuid(), Guid.NewGuid(), name);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void Validate_WithNameLongerThanTwoHundredCharacters_FailsOnName()
    {
        var command = new CreateProductCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 201));

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void Validate_WithEmptyCategoryId_FailsOnCategoryId()
    {
        var command = new CreateProductCommand(Guid.Empty, Guid.NewGuid(), "Sample Product");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateProductCommand.CategoryId));
    }

    [Fact]
    public void Validate_WithEmptyBrandId_FailsOnBrandId()
    {
        var command = new CreateProductCommand(Guid.NewGuid(), Guid.Empty, "Sample Product");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateProductCommand.BrandId));
    }
}
