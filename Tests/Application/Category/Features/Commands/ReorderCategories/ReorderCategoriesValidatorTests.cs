using Application.Category.Features.Commands.ReorderCategories;

namespace Tests.Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesValidatorTests
{
    private readonly ReorderCategoriesValidator _sut = new();

    [Fact]
    public void Validate_WithNonEmptyValidItems_HasNoErrors()
    {
        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>
    {
        (Guid.NewGuid(), 0),
        (Guid.NewGuid(), 1)
    });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyItems_HasErrorForItems()
    {
        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>());

        var result = _sut.Validate(command);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ReorderCategoriesCommand.Items) &&
            e.ErrorMessage == "لیست دسته‌بندی‌ها نمی‌تواند خالی باشد.");
    }

    [Fact]
    public void Validate_WithItemContainingEmptyId_HasError()
    {
        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>
    {
        (Guid.Empty, 0)
    });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Id"));
    }

    [Fact]
    public void Validate_WithItemContainingNegativeSortOrder_HasError()
    {
        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>
    {
        (Guid.NewGuid(), -1)
    });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("SortOrder"));
    }
}
