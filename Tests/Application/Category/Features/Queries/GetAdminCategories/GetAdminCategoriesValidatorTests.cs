using Application.Category.Features.Queries.GetAdminCategories;

namespace Tests.Application.Category.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesValidatorTests
{
    private readonly GetAdminCategoriesValidator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithPageLessThanOrEqualZero_HasErrorForPage(int page)
    {
        var query = new GetAdminCategoriesQuery(null, null, false, page, 10);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAdminCategoriesQuery.Page));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_WithPageSizeWithinRange_HasNoErrorForPageSize(int pageSize)
    {
        var query = new GetAdminCategoriesQuery(null, null, false, 1, pageSize);

        var result = _sut.Validate(query);

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(GetAdminCategoriesQuery.PageSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_HasErrorForPageSize(int pageSize)
    {
        var query = new GetAdminCategoriesQuery(null, null, false, 1, pageSize);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAdminCategoriesQuery.PageSize));
    }

    [Fact]
    public void Validate_WithValidPageAndPageSize_HasNoErrors()
    {
        var query = new GetAdminCategoriesQuery("term", true, false, 2, 50);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }
}
