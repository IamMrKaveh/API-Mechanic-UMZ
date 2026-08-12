using Application.Category.Contracts;
using Application.Category.Features.Queries.GetCategories;
using Application.Category.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Category.Features.Queries.GetCategories;

public class GetCategoriesHandlerTests
{
    private readonly ICategoryQueryService _queryService = Substitute.For<ICategoryQueryService>(); private readonly GetCategoriesHandler _sut;

    public GetCategoriesHandlerTests()
    {
        _sut = new GetCategoriesHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ForwardsQueryArgumentsToQueryService()
    {
        var expected = new PaginatedResult<CategoryListItemDto>(
            new List<CategoryListItemDto> { new() { Id = Guid.NewGuid(), Name = "A" } }.AsReadOnly(),
            1, 1, 10);

        _queryService
            .GetCategoriesPagedAsync("a", null, false, 1, 10, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetCategoriesQuery("a", null, false, 1, 10),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.TotalCount.ShouldBe(1);
    }
}
