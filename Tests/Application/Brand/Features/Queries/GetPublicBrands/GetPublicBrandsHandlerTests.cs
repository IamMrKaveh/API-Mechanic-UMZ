using Application.Brand.Contracts;
using Application.Brand.Features.Queries.GetPublicBrands;
using Application.Brand.Features.Shared;
using Domain.Category.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Brand.Features.Queries.GetPublicBrands;

public class GetPublicBrandsHandlerTests
{
    private readonly IBrandQueryService _queryService = Substitute.For<IBrandQueryService>(); private readonly GetPublicBrandsHandler _sut;

    public GetPublicBrandsHandlerTests()
    {
        _sut = new GetPublicBrandsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenCategoryIdMissing_ForwardsNullCategoryIdAndReturnsBrands()
    {
        var expected = new List<BrandListItemDto>
    {
        new() { Id = Guid.NewGuid(), Name = "A" },
        new() { Id = Guid.NewGuid(), Name = "B" }
    };
        _queryService
            .GetPublicBrandsAsync(Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetPublicBrandsQuery(null), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        await _queryService.Received(1).GetPublicBrandsAsync(
            Arg.Is<CategoryId?>(x => x == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryIdProvided_PassesCategoryIdBuiltFromRequestToQueryService()
    {
        var categoryGuid = Guid.NewGuid();
        CategoryId? captured = null;

        _queryService
            .GetPublicBrandsAsync(Arg.Do<CategoryId?>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(new List<BrandListItemDto>());

        _ = await _sut.Handle(new GetPublicBrandsQuery(categoryGuid), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(categoryGuid);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsEmptyList_ReturnsSuccessWithEmptyList()
    {
        _queryService
            .GetPublicBrandsAsync(Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(new List<BrandListItemDto>());

        var result = await _sut.Handle(new GetPublicBrandsQuery(null), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
