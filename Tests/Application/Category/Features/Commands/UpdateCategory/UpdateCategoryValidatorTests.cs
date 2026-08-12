using Application.Category.Features.Commands.UpdateCategory;

namespace Tests.Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _sut = new();

    private static UpdateCategoryCommand ValidCommand(
        Guid? id = null,
        string name = "Books",
        bool isActive = true,
        string? slug = null,
        string? description = null,
        int sortOrder = 0,
        string? rowVersion = null) =>
        new(id ?? Guid.NewGuid(), name, isActive, slug, description, sortOrder, rowVersion);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_HasErrorForId()
    {
        var result = _sut.Validate(ValidCommand(id: Guid.Empty));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCategoryCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullName_HasErrorForName(string? name)
    {
        var result = _sut.Validate(ValidCommand(name: name!));

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateCategoryCommand.Name) &&
            e.ErrorMessage == "نام دسته‌بندی الزامی است.");
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_HasErrorForName()
    {
        var name = new string('a', 101);

        var result = _sut.Validate(ValidCommand(name: name));

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateCategoryCommand.Name) &&
            e.ErrorMessage == "نام دسته‌بندی نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");
    }

    [Fact]
    public void Validate_WithSlugExceedingMaxLength_HasErrorForSlug()
    {
        var slug = new string('a', 201);

        var result = _sut.Validate(ValidCommand(slug: slug));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCategoryCommand.Slug));
    }

    [Fact]
    public void Validate_WithDescriptionExceedingMaxLength_HasErrorForDescription()
    {
        var description = new string('a', 1001);

        var result = _sut.Validate(ValidCommand(description: description));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCategoryCommand.Description));
    }

    [Fact]
    public void Validate_WithNegativeSortOrder_HasErrorForSortOrder()
    {
        var result = _sut.Validate(ValidCommand(sortOrder: -1));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCategoryCommand.SortOrder));
    }

    [Fact]
    public void Validate_WithNullRowVersion_HasNoErrorForRowVersion()
    {
        var result = _sut.Validate(ValidCommand(rowVersion: null));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(UpdateCategoryCommand.RowVersion));
    }

    [Fact]
    public void Validate_WithValidBase64RowVersion_HasNoErrorForRowVersion()
    {
        var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        var result = _sut.Validate(ValidCommand(rowVersion: validBase64));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(UpdateCategoryCommand.RowVersion));
    }

    [Fact]
    public void Validate_WithInvalidBase64RowVersion_HasErrorForRowVersion()
    {
        var result = _sut.Validate(ValidCommand(rowVersion: "not-base64!!!"));

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateCategoryCommand.RowVersion) &&
            e.ErrorMessage == "نسخه سطر نامعتبر است.");
    }
}
