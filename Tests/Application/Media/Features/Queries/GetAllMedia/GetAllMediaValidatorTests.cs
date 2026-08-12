using Application.Media.Features.Queries.GetAllMedia;

namespace Tests.Application.Media.Features.Queries.GetAllMedia;

public class GetAllMediaValidatorTests
{
    private readonly GetAllMediaValidator _sut = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 10)]
    [InlineData(5, 50)]
    [InlineData(int.MaxValue, 100)]
    public void Validate_WhenPageAndPageSizeAreWithinAllowedRange_IsValid(int page, int pageSize)
    {
        var query = new GetAllMediaQuery(null, page, pageSize);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_WhenPageIsNotGreaterThanZero_IsInvalidOnPage(int page)
    {
        var query = new GetAllMediaQuery(null, page, 10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAllMediaQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public void Validate_WhenPageSizeIsOutsideOneToOneHundred_IsInvalidOnPageSize(int pageSize)
    {
        var query = new GetAllMediaQuery(null, 1, pageSize);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAllMediaQuery.PageSize));
    }

    [Fact]
    public void Validate_WhenEntityTypeIsNull_IsValid()
    {
        var query = new GetAllMediaQuery(null, 1, 10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }
}
