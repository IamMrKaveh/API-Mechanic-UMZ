using Application.Category.Features.Commands.CreateCategory;

namespace Tests.Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _sut = new();

    [Fact]
    public void Validate_WithNonEmptyCategoryName_HasNoErrors()
    {
        var command = new CreateCategoryCommand("Electronics", null, null, 0);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullCategoryName_HasErrorForCategoryName(string? categoryName)
    {
        var command = new CreateCategoryCommand(categoryName!, null, null, 0);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateCategoryCommand.CategoryName));
    }

    [Fact]
    public void Validate_WithEmptyCategoryName_ProducesExpectedErrorMessage()
    {
        var command = new CreateCategoryCommand(string.Empty, null, null, 0);

        var result = _sut.Validate(command);

        var error = result.Errors.ShouldHaveSingleItem();
        error.PropertyName.ShouldBe(nameof(CreateCategoryCommand.CategoryName));
        error.ErrorMessage.ShouldBe("Category name is required.");
    }
}
