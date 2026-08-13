using Application.Product.Contracts;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Product.Features.Queries.GetProductCatalog;

public class GetProductCatalogHandlerTests
{
    private readonly IProductQueryService _productQueryService = Substitute.For<IProductQueryService>(); private readonly GetProductCatalogHandler _sut;

    public GetProductCatalogHandlerTests()
    {
        _sut = new GetProductCatalogHandler(_productQueryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithQueryServicePayload()
    {
        var expected = new PaginatedResult<ProductCatalogItemDto>(
            new List<ProductCatalogItemDto> { new() { Id = Guid.NewGuid(), Name = "Catalog Item" } },
            1,
            1,
            10);

        _productQueryService
            .GetProductCatalogAsync(Arg.Any<ProductCatalogSearchParams>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetProductCatalogQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_MapsQueryParametersToSearchParams()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var expected = new PaginatedResult<ProductCatalogItemDto>(new List<ProductCatalogItemDto>(), 0, 2, 25);

        _productQueryService
            .GetProductCatalogAsync(Arg.Any<ProductCatalogSearchParams>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetProductCatalogQuery(
            Page: 2,
            PageSize: 25,
            Search: "phone",
            CategoryId: categoryId,
            BrandId: brandId,
            MinPrice: 100_000m,
            MaxPrice: 900_000m,
            InStockOnly: true,
            SortBy: "price-asc",
            IsFeatured: true,
            HasDiscount: false);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _productQueryService.Received(1).GetProductCatalogAsync(
            Arg.Is<ProductCatalogSearchParams>(p =>
                p!.Page == 2 &&
                p.PageSize == 25 &&
                p.Search == "phone" &&
                p.CategoryId == categoryId &&
                p.BrandId == brandId &&
                p.MinPrice == 100_000m &&
                p.MaxPrice == 900_000m &&
                p.InStockOnly == true &&
                p.SortBy == "price-asc" &&
                p.IsFeatured == true &&
                p.HasDiscount == false),
            Arg.Any<CancellationToken>());
    }
}
