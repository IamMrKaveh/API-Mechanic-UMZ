using Application.Category.Contracts;
using Application.Category.Features.Queries.GetCategoryProducts;
using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Category.Features.Queries.GetCategoryProducts;

public class GetCategoryProductsHandlerTests
{
    private readonly ICategoryQueryService _queryService = Substitute.For<ICategoryQueryService>(); private readonly GetCategoryProductsHandler _sut;

    public GetCategoryProductsHandlerTests()
    {
        _sut = new GetCategoryProductsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WrapsCategoryIdAndForwardsParametersToQueryService()
    {
        var id = Guid.NewGuid();
        CategoryId? captured = null;
        var expected = new PaginatedResult<CategoryProductItemDto>(
            new List<CategoryProductItemDto>().AsReadOnly(), 0, 1, 10);

        _queryService
            .GetCategoryProductsAsync(
                Arg.Do<CategoryId>(x => captured = x),
                true, 1, 10, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetCategoryProductsQuery(id, true, 1, 10),
            CancellationToken.None);

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithPaginatedResultFromQueryService()
    {
        var items = new List<CategoryProductItemDto>
    {
        new() { Id = Guid.NewGuid(), Name = "P1", BrandName = "B" }
    };
        var expected = new PaginatedResult<CategoryProductItemDto>(items.AsReadOnly(), 1, 1, 10);

        _queryService
            .GetCategoryProductsAsync(
                Arg.Any<CategoryId>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetCategoryProductsQuery(Guid.NewGuid(), false, 1, 10),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Name.ShouldBe("P1");
    }
}
