using Application.Search.Features.Queries.GetSearchSuggestions;

namespace Tests.Application.Search.Features.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsValidatorTests
{
    private readonly GetSearchSuggestionsValidator _sut = new();

    [Fact]
    public void Validate_WithValidQuery_HasNoErrors()
    {
        var query = new GetSearchSuggestionsQuery("lap", 10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyQ_HasErrorForQ(string? q)
    {
        var query = new GetSearchSuggestionsQuery(q!, 10);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetSearchSuggestionsQuery.Q) &&
            e.ErrorMessage == "عبارت جستجو نمی‌تواند خالی باشد.");
    }

    [Fact]
    public void Validate_WithQShorterThanTwoCharacters_HasMinimumLengthErrorForQ()
    {
        var query = new GetSearchSuggestionsQuery("a", 10);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetSearchSuggestionsQuery.Q) &&
            e.ErrorMessage == "عبارت جستجو باید حداقل ۲ کاراکتر باشد.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public void Validate_WithMaxSuggestionsOutOfRange_HasErrorForMaxSuggestions(int maxSuggestions)
    {
        var query = new GetSearchSuggestionsQuery("lap", maxSuggestions);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetSearchSuggestionsQuery.MaxSuggestions) &&
            e.ErrorMessage == "تعداد پیشنهادات باید بین ۱ تا ۲۰ باشد.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    public void Validate_WithMaxSuggestionsInRange_HasNoErrorForMaxSuggestions(int maxSuggestions)
    {
        var query = new GetSearchSuggestionsQuery("lap", maxSuggestions);

        var result = _sut.Validate(query);

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(GetSearchSuggestionsQuery.MaxSuggestions));
    }
}
