using Application.Brand.Contracts;
using Application.Brand.Features.Queries.GetAdminBrands;
using Application.Brand.Features.Shared;
using Domain.Category.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Brand.Features.Queries.GetAdminBrands;

public class GetAdminBrandsHandlerTests
{
    private readonly IBrandQueryService _queryService = Substitute.For<IBrandQueryService>(); private readonly GetAdminBrandsHandler _sut;

    public GetAdminBrandsHandlerTests()
    {
        _sut = new GetAdminBrandsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ForwardsQueryArgumentsToQueryService()
    {
        var expected = new PaginatedResult<BrandListItemDto>(
            new List<BrandListItemDto> { new() { Id = Guid.NewGuid(), Name = "A" } }.AsReadOnly(),
            1, 2, 25);

        _queryService
            .GetBrandsPagedAsync(
                Arg.Any<CategoryId?>(),
                "s",
                false,
                true,
                2,
                25,
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetAdminBrandsQuery(Guid.NewGuid(), "s", false, true, 2, 25),
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
            new GetAdminBrandsQuery(categoryGuid, null, null, true, 1, 10),
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
            new GetAdminBrandsQuery(null, null, null, true, 1, 10),
            CancellationToken.None);

        captured.ShouldBeNull();
    }
}
