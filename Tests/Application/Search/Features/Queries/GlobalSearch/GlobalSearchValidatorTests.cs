using Application.Search.Features.Queries.GlobalSearch;

namespace Tests.Application.Search.Features.Queries.GlobalSearch;

public class GlobalSearchValidatorTests
{
    private readonly GlobalSearchValidator _sut = new();

    [Fact]
    public void Validate_WithValidQuery_HasNoErrors()
    {
        var query = new GlobalSearchQuery("shoes");

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyQ_HasErrorForQ(string? q)
    {
        var query = new GlobalSearchQuery(q!);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GlobalSearchQuery.Q) &&
            e.ErrorMessage == "عبارت جستجو نمی‌تواند خالی باشد.");
    }
}
