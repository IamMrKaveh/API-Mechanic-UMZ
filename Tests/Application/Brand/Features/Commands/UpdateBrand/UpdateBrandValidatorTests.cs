using Application.Brand.Features.Commands.UpdateBrand;

namespace Tests.Application.Brand.Features.Commands.UpdateBrand;

public class UpdateBrandValidatorTests
{
    private readonly UpdateBrandValidator _sut = new();

    private static UpdateBrandCommand ValidCommand(
        Guid? brandId = null,
        Guid? categoryId = null,
        string name = "Sony",
        string? slug = null,
        string? description = null,
        string? rowVersion = null) =>
        new(
            brandId ?? Guid.NewGuid(),
            categoryId ?? Guid.NewGuid(),
            name,
            slug,
            description,
            null, null, null, null,
            rowVersion);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyBrandId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(brandId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBrandCommand.BrandId));
    }

    [Fact]
    public void Validate_WithEmptyCategoryId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(categoryId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBrandCommand.CategoryId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingName_IsInvalid(string name)
    {
        var result = _sut.Validate(ValidCommand(name: name));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBrandCommand.Name));
    }

    [Fact]
    public void Validate_WithNameLongerThanMaximumLength_IsInvalid()
    {
        var longName = new string('a', 101);
        var result = _sut.Validate(ValidCommand(name: longName));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBrandCommand.Name));
    }

    [Fact]
    public void Validate_WithSlugLongerThanMaximumLength_IsInvalid()
    {
        var longSlug = new string('a', 201);
        var result = _sut.Validate(ValidCommand(slug: longSlug));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBrandCommand.Slug));
    }

    [Fact]
    public void Validate_WithDescriptionLongerThanMaximumLength_IsInvalid()
    {
        var longDescription = new string('a', 501);
        var result = _sut.Validate(ValidCommand(description: longDescription));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBrandCommand.Description));
    }

    [Fact]
    public void Validate_WithValidBase64RowVersion_IsValid()
    {
        var rowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        var result = _sut.Validate(ValidCommand(rowVersion: rowVersion));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not-base64!!")]
    [InlineData("###")]
    public void Validate_WithInvalidBase64RowVersion_IsInvalid(string rowVersion)
    {
        var result = _sut.Validate(ValidCommand(rowVersion: rowVersion));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateBrandCommand.RowVersion));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingRowVersion_IsValid(string? rowVersion)
    {
        var result = _sut.Validate(ValidCommand(rowVersion: rowVersion));

        result.IsValid.ShouldBeTrue();
    }
}
