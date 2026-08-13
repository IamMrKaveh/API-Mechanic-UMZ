using Application.Search.Features.Queries.SearchProducts;

namespace Tests.Application.Search.Features.Queries.SearchProducts;

public class SearchProductsValidatorTests
{
    private readonly SearchProductsValidator _sut = new();

    [Fact]
    public void Validate_WithValidQuery_HasNoErrors()
    {
        var query = new SearchProductsQuery(
            "laptop",
            Guid.NewGuid(),
            Guid.NewGuid(),
            10m,
            100m,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullPricesAndNullQ_HasNoErrors()
    {
        var query = new SearchProductsQuery(
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNegativeMinPrice_HasErrorForMinPrice(decimal minPrice)
    {
        var query = new SearchProductsQuery(
            "laptop",
            null,
            null,
            minPrice,
            null,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SearchProductsQuery.MinPrice) &&
            e.ErrorMessage == "حداقل قیمت نمی‌تواند منفی باشد.");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNegativeMaxPrice_HasErrorForMaxPrice(decimal maxPrice)
    {
        var query = new SearchProductsQuery(
            "laptop",
            null,
            null,
            null,
            maxPrice,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SearchProductsQuery.MaxPrice) &&
            e.ErrorMessage == "حداکثر قیمت نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void Validate_WithMinPriceGreaterThanMaxPrice_HasRangeError()
    {
        var query = new SearchProductsQuery(
            "laptop",
            null,
            null,
            200m,
            100m,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.Errors.ShouldContain(e =>
            e.ErrorMessage == "حداقل قیمت نمی‌تواند بیشتر از حداکثر قیمت باشد.");
    }

    [Fact]
    public void Validate_WithMinPriceEqualToMaxPrice_HasNoRangeError()
    {
        var query = new SearchProductsQuery(
            "laptop",
            null,
            null,
            100m,
            100m,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyMinPriceSet_DoesNotTriggerRangeError()
    {
        var query = new SearchProductsQuery(
            "laptop",
            null,
            null,
            50m,
            null,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyMaxPriceSet_DoesNotTriggerRangeError()
    {
        var query = new SearchProductsQuery(
            "laptop",
            null,
            null,
            null,
            50m,
            false,
            null,
            1,
            10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }
}
