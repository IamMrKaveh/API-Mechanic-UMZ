using Application.Product.Contracts;
using Application.Product.Features.Queries.GetProducts;
using Application.Product.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Product.Features.Queries.GetProducts;

public class GetProductsHandlerTests
{
    private readonly IProductQueryService _productQueryService = Substitute.For<IProductQueryService>(); private readonly GetProductsHandler _sut;

    public GetProductsHandlerTests()
    {
        _sut = new GetProductsHandler(_productQueryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithQueryServicePayload()
    {
        var expected = new PaginatedResult<ProductListItemDto>(
            new List<ProductListItemDto> { new() { Id = Guid.NewGuid(), Name = "P1" } },
            1,
            1,
            10);

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

        var query = new GetProductsQuery(null, null, null, null, false, 1, 10);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesQueryParametersToQueryService()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var expected = new PaginatedResult<ProductListItemDto>(new List<ProductListItemDto>(), 0, 2, 25);

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

        var query = new GetProductsQuery(categoryId, brandId, "term", true, true, 2, 25);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _productQueryService.Received(1).GetAdminProductsAsync(
            categoryId,
            brandId,
            "term",
            true,
            true,
            2,
            25,
            Arg.Any<CancellationToken>());
    }
}
