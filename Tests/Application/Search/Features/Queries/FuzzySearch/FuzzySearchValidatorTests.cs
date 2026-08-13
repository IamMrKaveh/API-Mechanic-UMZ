using Application.Search.Features.Queries.FuzzySearch;

namespace Tests.Application.Search.Features.Queries.FuzzySearch;

public class FuzzySearchValidatorTests
{
    private readonly FuzzySearchValidator _sut = new();

    [Fact]
    public void Validate_WithValidQuery_HasNoErrors()
    {
        var query = new FuzzySearchQuery("laptop", 1, 10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyQ_HasErrorForQ(string? q)
    {
        var query = new FuzzySearchQuery(q!, 1, 10);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(FuzzySearchQuery.Q) &&
            e.ErrorMessage == "عبارت جستجو نمی‌تواند خالی باشد.");
    }
}
