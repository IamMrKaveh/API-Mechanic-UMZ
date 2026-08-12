using Application.Category.Features.Queries.GetCategoryProducts;

namespace Tests.Application.Category.Features.Queries.GetCategoryProducts;

public class GetCategoryProductsValidatorTests
{
    private readonly GetCategoryProductsValidator _sut = new();

    [Fact]
    public void Validate_WithValidQuery_HasNoErrors()
    {
        var query = new GetCategoryProductsQuery(Guid.NewGuid(), true, 1, 20);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCategoryId_HasErrorForCategoryId()
    {
        var query = new GetCategoryProductsQuery(Guid.Empty, true, 1, 20);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetCategoryProductsQuery.CategoryId) &&
            e.ErrorMessage == "شناسه دسته‌بندی الزامی است.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithPageLessThanOrEqualZero_HasErrorForPage(int page)
    {
        var query = new GetCategoryProductsQuery(Guid.NewGuid(), true, page, 20);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetCategoryProductsQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_HasErrorForPageSize(int pageSize)
    {
        var query = new GetCategoryProductsQuery(Guid.NewGuid(), true, 1, pageSize);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetCategoryProductsQuery.PageSize));
    }
}
