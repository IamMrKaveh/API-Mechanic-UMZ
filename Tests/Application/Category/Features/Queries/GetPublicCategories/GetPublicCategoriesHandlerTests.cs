using Application.Category.Contracts;
using Application.Category.Features.Queries.GetPublicCategories;
using Application.Category.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Category.Features.Queries.GetPublicCategories;

public class GetPublicCategoriesHandlerTests
{
    private readonly ICategoryQueryService _queryService = Substitute.For<ICategoryQueryService>(); private readonly GetPublicCategoriesHandler _sut;

    public GetPublicCategoriesHandlerTests()
    {
        _sut = new GetPublicCategoriesHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ForwardsSearchAndPaginationToQueryService()
    {
        var expected = new PaginatedResult<CategoryDto>(
            new List<CategoryDto>().AsReadOnly(), 0, 3, 15);

        _queryService
            .GetPublicCategoriesAsync("q", 3, 15, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetPublicCategoriesQuery("q", 3, 15),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        await _queryService.Received(1).GetPublicCategoriesAsync(
            "q", 3, 15, Arg.Any<CancellationToken>());
    }
}
