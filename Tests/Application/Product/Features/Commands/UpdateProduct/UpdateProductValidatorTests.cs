using Application.Product.Features.Commands.UpdateProduct;

namespace Tests.Application.Product.Features.Commands.UpdateProduct;

public class UpdateProductValidatorTests
{
    private readonly UpdateProductValidator _sut = new();

    private static UpdateProductCommand ValidCommand(
        Guid? id = null,
        Guid? categoryId = null,
        Guid? brandId = null,
        string name = "Sample Product",
        string slug = "sample-product",
        string? description = null,
        bool isActive = true,
        bool isFeatured = false,
        string rowVersion = "AAAAAAAAB9E=")
        => new(
            id ?? Guid.NewGuid(),
            categoryId ?? Guid.NewGuid(),
            brandId ?? Guid.NewGuid(),
            name,
            slug,
            description,
            isActive,
            isFeatured,
            rowVersion);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_FailsOnId()
    {
        var result = _sut.Validate(ValidCommand(id: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_FailsOnName(string name)
    {
        var result = _sut.Validate(ValidCommand(name: name));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductCommand.Name));
    }

    [Fact]
    public void Validate_WithNameLongerThanTwoHundredCharacters_FailsOnName()
    {
        var result = _sut.Validate(ValidCommand(name: new string('a', 201)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductCommand.Name));
    }

    [Fact]
    public void Validate_WithEmptyCategoryId_FailsOnCategoryId()
    {
        var result = _sut.Validate(ValidCommand(categoryId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductCommand.CategoryId));
    }

    [Fact]
    public void Validate_WithEmptyBrandId_FailsOnBrandId()
    {
        var result = _sut.Validate(ValidCommand(brandId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductCommand.BrandId));
    }

    [Fact]
    public void Validate_WithEmptyRowVersion_FailsOnRowVersion()
    {
        var result = _sut.Validate(ValidCommand(rowVersion: string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductCommand.RowVersion));
    }

    [Fact]
    public void Validate_WithSlugLongerThanTwoHundredCharacters_FailsOnSlug()
    {
        var result = _sut.Validate(ValidCommand(slug: new string('a', 201)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductCommand.Slug));
    }
}
