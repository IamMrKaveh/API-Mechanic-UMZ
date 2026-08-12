using Application.Product.Features.Queries.GetAdminProducts;

namespace Tests.Application.Product.Features.Queries.GetAdminProducts;

public class GetAdminProductsValidatorTests
{
    private readonly GetAdminProductsValidator _sut = new();

    private static GetAdminProductsQuery Query(int page = 1, int pageSize = 20)
        => new(null, null, null, null, false, page, pageSize);

    [Fact]
    public void Validate_WithValidPagination_IsValid()
    {
        var result = _sut.Validate(Query());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositivePage_FailsOnPage(int page)
    {
        var result = _sut.Validate(Query(page: page));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAdminProductsQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Validate_WithPageSizeOutOfRange_FailsOnPageSize(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAdminProductsQuery.PageSize));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_WithPageSizeWithinRange_IsValid(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(GetAdminProductsQuery.PageSize));
    }
}
