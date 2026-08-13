using Application.Brand.Features.Queries.GetAdminBrands;

namespace Tests.Application.Brand.Features.Queries.GetAdminBrands;

public class GetAdminBrandsValidatorTests
{
    private readonly GetAdminBrandsValidator _sut = new();

    private static GetAdminBrandsQuery Query(int page = 1, int pageSize = 10) =>
        new(null, null, null, false, page, pageSize);

    [Fact]
    public void Validate_WithValidQuery_IsValid()
    {
        var result = _sut.Validate(Query());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithPageNotGreaterThanZero_IsInvalid(int page)
    {
        var result = _sut.Validate(Query(page: page));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAdminBrandsQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Validate_WithPageSizeOutsideAllowedRange_IsInvalid(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAdminBrandsQuery.PageSize));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_WithPageSizeAtBoundary_IsValid(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.IsValid.ShouldBeTrue();
    }
}
