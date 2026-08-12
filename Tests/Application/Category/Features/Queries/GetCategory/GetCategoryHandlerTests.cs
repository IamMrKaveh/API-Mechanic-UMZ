using Application.Category.Contracts;
using Application.Category.Features.Queries.GetCategory;
using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Category.Features.Queries.GetCategory;

public class GetCategoryHandlerTests
{
    private readonly ICategoryQueryService _queryService = Substitute.For<ICategoryQueryService>(); private readonly GetCategoryHandler _sut;

    public GetCategoryHandlerTests()
    {
        _sut = new GetCategoryHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsNull_ReturnsNotFound()
    {
        _queryService
            .GetCategoryDetailAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((CategoryDetailDto?)null);

        var result = await _sut.Handle(new GetCategoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsDto_ReturnsSuccessWithDto()
    {
        var dto = new CategoryDetailDto { Id = Guid.NewGuid(), Name = "Books" };
        _queryService
            .GetCategoryDetailAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetCategoryQuery(dto.Id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_PassesRequestIdWrappedInCategoryIdToQueryService()
    {
        var id = Guid.NewGuid();
        CategoryId? captured = null;
        _queryService
            .GetCategoryDetailAsync(Arg.Do<CategoryId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto { Id = id, Name = "n" });

        await _sut.Handle(new GetCategoryQuery(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
