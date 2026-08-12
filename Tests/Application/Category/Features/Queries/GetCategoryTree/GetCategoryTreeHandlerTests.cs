using Application.Category.Contracts;
using Application.Category.Features.Queries.GetCategoryTree;
using Application.Category.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Category.Features.Queries.GetCategoryTree;

public class GetCategoryTreeHandlerTests
{
    private readonly ICategoryQueryService _queryService = Substitute.For<ICategoryQueryService>(); private readonly GetCategoryTreeHandler _sut;

    public GetCategoryTreeHandlerTests()
    {
        _sut = new GetCategoryTreeHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithTreeFromQueryService()
    {
        var tree = new List<CategoryTreeDto>
    {
        new() { Id = Guid.NewGuid(), Name = "Root", Slug = "root" }
    };
        _queryService
            .GetCategoryTreeAsync(Arg.Any<CancellationToken>())
            .Returns(tree);

        var result = await _sut.Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("Root");
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsEmpty_ReturnsSuccessWithEmpty()
    {
        _queryService
            .GetCategoryTreeAsync(Arg.Any<CancellationToken>())
            .Returns(new List<CategoryTreeDto>());

        var result = await _sut.Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
