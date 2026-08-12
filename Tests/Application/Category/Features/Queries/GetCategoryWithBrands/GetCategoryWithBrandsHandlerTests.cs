using Application.Category.Contracts;
using Application.Category.Features.Queries.GetCategoryWithBrands;
using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Category.Features.Queries.GetCategoryWithBrands;

public class GetCategoryWithBrandsHandlerTests
{
    private readonly ICategoryQueryService _queryService = Substitute.For<ICategoryQueryService>(); private readonly GetCategoryWithBrandsHandler _sut;

    public GetCategoryWithBrandsHandlerTests()
    {
        _sut = new GetCategoryWithBrandsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsNull_ReturnsNotFound()
    {
        _queryService
            .GetCategoryWithBrandsAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((CategoryWithBrandsDto?)null);

        var result = await _sut.Handle(
            new GetCategoryWithBrandsQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsDto_ReturnsSuccessWithDto()
    {
        var dto = new CategoryWithBrandsDto { Id = Guid.NewGuid(), Name = "Cars" };
        _queryService
            .GetCategoryWithBrandsAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(
            new GetCategoryWithBrandsQuery(dto.Id),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_WrapsRequestCategoryIdInCategoryIdAndForwardsToService()
    {
        var id = Guid.NewGuid();
        CategoryId? captured = null;
        _queryService
            .GetCategoryWithBrandsAsync(
                Arg.Do<CategoryId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns(new CategoryWithBrandsDto { Id = id, Name = "n" });

        await _sut.Handle(new GetCategoryWithBrandsQuery(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
