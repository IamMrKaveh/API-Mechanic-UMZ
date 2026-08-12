using Application.Category.Contracts;
using Application.Category.Features.Queries.GetAdminCategories;
using Application.Category.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Category.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesHandlerTests
{
    private readonly ICategoryQueryService _queryService = Substitute.For<ICategoryQueryService>(); private readonly GetAdminCategoriesHandler _sut;

    public GetAdminCategoriesHandlerTests()
    {
        _sut = new GetAdminCategoriesHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ForwardsQueryArgumentsToQueryService()
    {
        var page = 2;
        var pageSize = 25;
        var expected = new PaginatedResult<CategoryListItemDto>(
            new List<CategoryListItemDto>().AsReadOnly(), 0, page, pageSize);

        _queryService
            .GetCategoriesPagedAsync("term", true, true, page, pageSize, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetAdminCategoriesQuery("term", true, true, page, pageSize),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        await _queryService.Received(1).GetCategoriesPagedAsync(
            "term", true, true, page, pageSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsEmpty_ReturnsSuccessWithZeroTotal()
    {
        var empty = new PaginatedResult<CategoryListItemDto>(
            new List<CategoryListItemDto>().AsReadOnly(), 0, 1, 10);

        _queryService
            .GetCategoriesPagedAsync(
                Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        var result = await _sut.Handle(
            new GetAdminCategoriesQuery(null, null, false, 1, 10),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Items.ShouldBeEmpty();
    }
}
