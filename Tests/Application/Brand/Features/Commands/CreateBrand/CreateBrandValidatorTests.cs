using Application.Brand.Features.Commands.CreateBrand;

namespace Tests.Application.Brand.Features.Commands.CreateBrand;

public class CreateBrandValidatorTests
{
    private readonly CreateBrandValidator _sut = new();

    private static CreateBrandCommand ValidCommand(
        Guid? categoryId = null,
        string name = "Sony",
        string? slug = null,
        string? description = null) =>
        new(
            categoryId ?? Guid.NewGuid(),
            name,
            slug,
            description,
            null, null, null, null);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingName_IsInvalid(string name)
    {
        var result = _sut.Validate(ValidCommand(name: name));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateBrandCommand.Name));
    }

    [Fact]
    public void Validate_WithEmptyCategoryId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(categoryId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateBrandCommand.CategoryId));
    }

    [Fact]
    public void Validate_WithNameLongerThanMaximumLength_IsInvalid()
    {
        var longName = new string('a', 101);
        var result = _sut.Validate(ValidCommand(name: longName));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateBrandCommand.Name));
    }

    [Fact]
    public void Validate_WithSlugLongerThanMaximumLength_IsInvalid()
    {
        var longSlug = new string('a', 201);
        var result = _sut.Validate(ValidCommand(slug: longSlug));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateBrandCommand.Slug));
    }

    [Fact]
    public void Validate_WithDescriptionLongerThanMaximumLength_IsInvalid()
    {
        var longDescription = new string('a', 501);
        var result = _sut.Validate(ValidCommand(description: longDescription));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateBrandCommand.Description));
    }
}
