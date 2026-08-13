using Application.Search.Contracts;
using Application.Search.Features.Queries.SearchProducts;
using Application.Search.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Search.Features.Queries.SearchProducts;

public class SearchProductsHandlerTests
{
    private readonly ISearchService _searchService = Substitute.For<ISearchService>(); private readonly SearchProductsHandler _sut;

    public SearchProductsHandlerTests()
    {
        _sut = new SearchProductsHandler(_searchService);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsResults_ReturnsSuccessWithSameResult()
    {
        var expected = new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = new List<ProductSearchResultItemDto>
        {
            new() { ProductId = Guid.NewGuid(), Name = "laptop" }
        },
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        _searchService
            .SearchProductsAsync(Arg.Any<SearchProductsParams>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new SearchProductsQuery(
            "laptop",
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            500m,
            true,
            "price_asc",
            1,
            10);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_MapsAllQueryFieldsIntoSearchProductsParams()
    {
        SearchProductsParams? captured = null;
        _searchService
            .SearchProductsAsync(Arg.Do<SearchProductsParams>(p => captured = p), Arg.Any<CancellationToken>())
            .Returns(new SearchResultDto<ProductSearchResultItemDto>
            {
                Items = new List<ProductSearchResultItemDto>(),
                Total = 0,
                Page = 2,
                PageSize = 25
            });

        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var ct = new CancellationTokenSource().Token;

        var query = new SearchProductsQuery(
            "laptop",
            categoryId,
            brandId,
            100m,
            500m,
            true,
            "price_desc",
            2,
            25);

        await _sut.Handle(query, ct);

        captured.ShouldNotBeNull();
        captured!.Q.ShouldBe("laptop");
        captured.CategoryId.ShouldBe(categoryId);
        captured.BrandId.ShouldBe(brandId);
        captured.MinPrice.ShouldBe(100m);
        captured.MaxPrice.ShouldBe(500m);
        captured.InStockOnly.ShouldBeTrue();
        captured.SortBy.ShouldBe("price_desc");
        captured.Page.ShouldBe(2);
        captured.PageSize.ShouldBe(25);

        await _searchService.Received(1).SearchProductsAsync(Arg.Any<SearchProductsParams>(), ct);
    }

    [Fact]
    public async Task Handle_WhenQueryQIsNull_MapsQToEmptyString()
    {
        SearchProductsParams? captured = null;
        _searchService
            .SearchProductsAsync(Arg.Do<SearchProductsParams>(p => captured = p), Arg.Any<CancellationToken>())
            .Returns(new SearchResultDto<ProductSearchResultItemDto>
            {
                Items = new List<ProductSearchResultItemDto>(),
                Total = 0,
                Page = 1,
                PageSize = 10
            });

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

        await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Q.ShouldBe(string.Empty);
        captured.CategoryId.ShouldBeNull();
        captured.BrandId.ShouldBeNull();
        captured.MinPrice.ShouldBeNull();
        captured.MaxPrice.ShouldBeNull();
        captured.InStockOnly.ShouldBeFalse();
        captured.SortBy.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsEmpty_ReturnsSuccessWithEmptyItems()
    {
        var expected = new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = new List<ProductSearchResultItemDto>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _searchService
            .SearchProductsAsync(Arg.Any<SearchProductsParams>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new SearchProductsQuery(
            "xyz",
            null,
            null,
            null,
            null,
            false,
            null,
            1,
            10);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.Total.ShouldBe(0);
    }
}
