using Application.Search.Features.Shared;
using Infrastructure.Search.Services;

namespace Tests.Infrastructure.Search.Services;

public class NoOpSearchServiceTests
{
    private readonly NoOpSearchService _sut = new();

    [Fact]
    public async Task IndexProductAsync_CompletesWithoutThrowing()
    {
        var document = new ProductSearchDocument { ProductId = Guid.NewGuid(), Name = "P" };

        await _sut.IndexProductAsync(document, CancellationToken.None);
    }

    [Fact]
    public async Task IndexCategoryAsync_CompletesWithoutThrowing()
    {
        var document = new CategorySearchDocument { CategoryId = Guid.NewGuid(), Name = "C" };

        await _sut.IndexCategoryAsync(document, CancellationToken.None);
    }

    [Fact]
    public async Task IndexBrandAsync_CompletesWithoutThrowing()
    {
        var document = new BrandSearchDocument { BrandId = Guid.NewGuid(), Name = "B" };

        await _sut.IndexBrandAsync(document, CancellationToken.None);
    }

    [Fact]
    public async Task SearchProductsAsync_ReturnsEmptyResultPreservingPaging()
    {
        var searchParams = new SearchProductsParams { Q = "brake", Page = 2, PageSize = 15 };

        var result = await _sut.SearchProductsAsync(searchParams, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.Total.ShouldBe(0);
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(15);
    }

    [Fact]
    public async Task SearchGlobalAsync_EchoesQueryWithEmptyProducts()
    {
        var result = await _sut.SearchGlobalAsync("oil filter", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Query.ShouldBe("oil filter");
        result.Products.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmptyList()
    {
        var result = await _sut.GetSuggestionsAsync("bre", maxSuggestions: 5, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchWithFuzzyAsync_ReturnsEmptyResultPreservingPaging()
    {
        var result = await _sut.SearchWithFuzzyAsync("brkae", page: 3, pageSize: 7, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.Total.ShouldBe(0);
        result.Page.ShouldBe(3);
        result.PageSize.ShouldBe(7);
    }

    [Fact]
    public async Task GetIndexStatsAsync_ReturnsZeroedStats()
    {
        var result = await _sut.GetIndexStatsAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result!.ProductsCount.ShouldBe(0);
        result.CategoriesCount.ShouldBe(0);
        result.BrandsCount.ShouldBe(0);
        result.TotalDocuments.ShouldBe(0);
    }
}
