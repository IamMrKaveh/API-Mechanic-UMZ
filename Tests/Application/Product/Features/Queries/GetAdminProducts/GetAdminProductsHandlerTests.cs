using Application.Product.Contracts;
using Application.Product.Features.Queries.GetAdminProducts;
using Application.Product.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Product.Features.Queries.GetAdminProducts;

public class GetAdminProductsHandlerTests
{
    private readonly IProductQueryService _productQueryService = Substitute.For<IProductQueryService>(); private readonly GetAdminProductsHandler _sut;

    public GetAdminProductsHandlerTests()
    {
        _sut = new GetAdminProductsHandler(_productQueryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithQueryServicePayload()
    {
        var expected = new PaginatedResult<ProductListItemDto>(
            new List<ProductListItemDto> { new() { Id = Guid.NewGuid(), Name = "Admin P1" } },
            1,
            1,
            20);

        _productQueryService
            .GetAdminProductsAsync(
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetAdminProductsQuery(null, null, null, null, false);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesQueryParametersToQueryService()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var expected = new PaginatedResult<ProductListItemDto>(new List<ProductListItemDto>(), 0, 3, 15);

        _productQueryService
            .GetAdminProductsAsync(
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetAdminProductsQuery(categoryId, brandId, "search", false, true, 3, 15);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _productQueryService.Received(1).GetAdminProductsAsync(
            categoryId,
            brandId,
            "search",
            false,
            true,
            3,
            15,
            Arg.Any<CancellationToken>());
    }
}
