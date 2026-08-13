using Application.Brand.Contracts;
using Application.Brand.Features.Queries.GetBrands;
using Application.Brand.Features.Shared;
using Domain.Category.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Brand.Features.Queries.GetBrands;

public class GetBrandsHandlerTests
{
    private readonly IBrandQueryService _queryService = Substitute.For<IBrandQueryService>(); private readonly GetBrandsHandler _sut;

    public GetBrandsHandlerTests()
    {
        _sut = new GetBrandsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ForwardsQueryArgumentsToQueryService()
    {
        var expected = new PaginatedResult<BrandListItemDto>(
            new List<BrandListItemDto> { new() { Id = Guid.NewGuid(), Name = "A" } }.AsReadOnly(),
            1, 1, 10);
        var categoryGuid = Guid.NewGuid();

        _queryService
            .GetBrandsPagedAsync(
                Arg.Any<CategoryId?>(),
                "search",
                true,
                false,
                1,
                10,
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetBrandsQuery(categoryGuid, "search", true, false, 1, 10),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenCategoryIdProvided_PassesCategoryIdBuiltFromRequestToQueryService()
    {
        var categoryGuid = Guid.NewGuid();
        CategoryId? captured = null;

        _queryService
            .GetBrandsPagedAsync(
                Arg.Do<CategoryId?>(x => captured = x),
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<BrandListItemDto>(new List<BrandListItemDto>().AsReadOnly(), 0, 1, 10));

        _ = await _sut.Handle(
            new GetBrandsQuery(categoryGuid, null, null, false, 1, 10),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(categoryGuid);
    }

    [Fact]
    public async Task Handle_WhenCategoryIdMissing_PassesNullCategoryIdToQueryService()
    {
        CategoryId? captured = CategoryId.NewId();
        _queryService
            .GetBrandsPagedAsync(
                Arg.Do<CategoryId?>(x => captured = x),
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<BrandListItemDto>(new List<BrandListItemDto>().AsReadOnly(), 0, 1, 10));

        _ = await _sut.Handle(
            new GetBrandsQuery(null, null, null, false, 1, 10),
            CancellationToken.None);

        captured.ShouldBeNull();
    }
}
