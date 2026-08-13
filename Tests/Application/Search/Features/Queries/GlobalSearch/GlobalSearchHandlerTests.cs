using Application.Search.Contracts;
using Application.Search.Features.Queries.GlobalSearch;
using Application.Search.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Search.Features.Queries.GlobalSearch;

public class GlobalSearchHandlerTests
{
    private readonly ISearchService _searchService = Substitute.For<ISearchService>(); private readonly GlobalSearchHandler _sut;

    public GlobalSearchHandlerTests()
    {
        _sut = new GlobalSearchHandler(_searchService);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsResults_ReturnsSuccessWithSameResult()
    {
        var expected = new GlobalSearchResultDto
        {
            Query = "shoes",
            Products = new List<ProductSearchResultItemDto>
        {
            new() { ProductId = Guid.NewGuid(), Name = "Running Shoes" }
        },
            Categories = new List<CategorySearchSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Footwear" }
        },
            Brands = new List<BrandSearchSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Nike" }
        }
        };

        _searchService
            .SearchGlobalAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GlobalSearchQuery("shoes"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ForwardsQueryParametersToSearchService()
    {
        _searchService
            .SearchGlobalAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GlobalSearchResultDto { Query = "shoes" });

        var ct = new CancellationTokenSource().Token;
        var query = new GlobalSearchQuery("shoes");

        await _sut.Handle(query, ct);

        await _searchService.Received(1).SearchGlobalAsync("shoes", ct);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsEmptyGroups_ReturnsSuccessWithEmptyCollections()
    {
        var expected = new GlobalSearchResultDto { Query = "xyz" };

        _searchService
            .SearchGlobalAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GlobalSearchQuery("xyz"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Products.ShouldBeEmpty();
        result.Value.Categories.ShouldBeEmpty();
        result.Value.Brands.ShouldBeEmpty();
    }
}
